using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Diagnostics;

namespace MergeSvnRepos
{
	class ContributingRepository
	{
		RevisionMap m_revmap;
		string      m_path;
		DateTime[]  m_dates;
		int         m_cur_rev;

		public ContributingRepository(string path)
		{
			m_revmap  = new RevisionMap();
			m_path    = path;
			m_dates   = GetDateList(path);
			m_cur_rev = 1;
		}

		public RevisionMap RevisionMap
		{
			get { return m_revmap; }
		}

		public int CurrentRevision
		{
			get { return m_cur_rev; }
		}

		public DateTime CurrentRevisionDate
		{
			get
			{
				if( m_cur_rev >= m_dates.Length )
					return DateTime.MaxValue;
				else
					return m_dates[m_cur_rev - 1];
			}
		}

		public string Path
		{
			get { return m_path; }
		}

		public byte[] GetCurrentRevision()
		{
			byte[] ret;

			using( Process p = new Process() )
			{
				ProcessStartInfo psi = new ProcessStartInfo();

				psi.Arguments              = string.Format("dump {0} --incremental --revision {1}", m_path, m_cur_rev);
				psi.CreateNoWindow         = true;
				psi.FileName               = "svnadmin";
				psi.RedirectStandardOutput = true;
				psi.UseShellExecute        = false;

				p.StartInfo = psi;

				p.Start();

				Stream s = p.StandardOutput.BaseStream;

				using( MemoryStream ms = new MemoryStream() )
				{
					byte[] buf = new byte[1024];

					while( !p.HasExited )
					{
						int read = s.Read(buf, 0, buf.Length);

						ms.Write(buf, 0, read);
					}

					ret = ms.ToArray();
				}

				p.Close();
			}

			// move to the next revision
			m_cur_rev++;

			return ret;
		}

		private DateTime[] GetDateList(string path)
		{
			DirectoryInfo di = new DirectoryInfo(path);

			if( !di.Exists )
				throw new ApplicationException("Repository does not exist: " + path);

			Console.WriteLine("Loading revision dates from {0}...", path);

			string      url = string.Format("file:///{0}", di.FullName.Replace('\\', '/'));
			XmlDocument xd  = new XmlDocument();

			using( Process p = new Process() )
			{
				StringBuilder    sb  = new StringBuilder();
				ProcessStartInfo psi = new ProcessStartInfo();

				psi.Arguments              = "log " + url + " --xml --revision 0:head --non-interactive";
				psi.CreateNoWindow         = true;
				psi.FileName               = "svn";
				psi.RedirectStandardOutput = true;
				psi.UseShellExecute        = false;

				p.StartInfo = psi;

				p.Start();

				p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { sb.AppendLine(e.Data); };

				p.BeginOutputReadLine();

				p.WaitForExit();
				p.Close();

				xd.LoadXml(sb.ToString());
			}

			XmlNodeList nodes = xd.SelectNodes("/log/logentry/date/.");
			DateTime[]  ret   = new DateTime[nodes.Count];

			for( int i = 0; i < ret.Length; i++ )
				ret[i] = DateTime.Parse(nodes[i].FirstChild.Value).ToUniversalTime();

			return ret;
		}

	}
}
