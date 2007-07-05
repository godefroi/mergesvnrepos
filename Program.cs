using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace MergeSvnRepos
{
	class Program
	{
		static void Main(string[] args)
		{
			long   global_rev    = 0;
			string new_repo_path = args[0];

			List<ContributingRepository> repo_list = new List<ContributingRepository>();

			for( int i = 1; i < args.Length; i++ )
				repo_list.Add(new ContributingRepository(args[i]));

			ContributingRepository[] repos = repo_list.ToArray();

			// create the parent dirs
			foreach( ContributingRepository repo in repos )
			{
				// create the dir
				CreateDirectory(new_repo_path, repo.Path);

				// increment the global rev
				global_rev++;
			}

			while( true )
			{
				// find the next repository
				ContributingRepository next_repo = FindNextRevision(repos);

				// check to see if we're done
				if( next_repo.CurrentRevisionDate == DateTime.MaxValue )
				{
					Console.WriteLine("Done!");
					return;
				}

				// display a pretty notice
				Console.WriteLine("Taking next revision ({0}) from {1}", next_repo.CurrentRevision, next_repo.Path);

				// increment the global revision
				global_rev++;

				// map the revision
				next_repo.RevisionMap.AddRevisionMapping(next_repo.CurrentRevision, global_rev);

				byte[] rev_to_load;

				// dump and process the revision
				using( MemoryStream input = new MemoryStream(next_repo.GetCurrentRevision()) )
				{
					using( MemoryStream output = new MemoryStream() )
					{
						DumpProcessor.ProcessDump(input, output, next_repo.RevisionMap, global_rev);

						rev_to_load = output.ToArray();
					}
				}

				// and, load the revision
				LoadRevision(new_repo_path, rev_to_load, next_repo.Path);
			}
		}

		static ContributingRepository FindNextRevision(ContributingRepository[] repos)
		{
			ContributingRepository cur_repo = repos[0];

			for( int i = 1; i < repos.Length; i++ )
			{
				if( repos[i].CurrentRevisionDate < cur_repo.CurrentRevisionDate )
					cur_repo = repos[i];
			}

			return cur_repo;
		}

		static void LoadRevision(string path, byte[] data, string reponame)
		{
			reponame = Path.GetFileName(reponame);

			using( Process p = new Process() )
			{
				ProcessStartInfo psi = new ProcessStartInfo();

				psi.Arguments              = string.Format("load {0} --ignore-uuid --parent-dir /{1}", path, reponame);
				psi.CreateNoWindow         = false;
				psi.FileName               = "svnadmin";
				psi.RedirectStandardOutput = false;
				psi.RedirectStandardInput  = true;
				psi.UseShellExecute        = false;

				p.StartInfo = psi;

				p.Start();

				p.StandardInput.BaseStream.Write(data, 0, data.Length);
				p.StandardInput.BaseStream.Flush();
				p.StandardInput.BaseStream.Close();

				p.WaitForExit();
				p.Close();
			}
		}

		static void CreateDirectory(string repo, string path)
		{
			DirectoryInfo di = new DirectoryInfo(repo);

			if( !di.Exists )
				throw new ApplicationException("Repository does not exist: " + repo);

			path = Path.GetFileName(path);

			Console.WriteLine("Creating parent dir {0} in {1}...", path, repo);

			string url = string.Format("file:///{0}/{1}", di.FullName.Replace('\\', '/'), path);

			using( Process p = new Process() )
			{
				ProcessStartInfo psi = new ProcessStartInfo();

				psi.Arguments              = "mkdir " + url + " --non-interactive -m \"Created directory to hold merged-in repository\" --username MergeSvnRepos";
				psi.CreateNoWindow         = true;
				psi.FileName               = "svn";
				psi.RedirectStandardOutput = false;
				psi.UseShellExecute        = false;

				p.StartInfo = psi;

				p.Start();
				p.WaitForExit();
				p.Close();
			}
		}

	}
}
