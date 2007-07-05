using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MergeSvnRepos
{
	class DumpProcessor
	{
		public static void ProcessDump(Stream input, Stream output, RevisionMap map, long expected_rev)
		{
			// skip to the first revision
			while( !PeekLine(input).StartsWith("Revision-number: ") )
				TransferLine(input, output);

			// now, we're ready to read revisions
			while( input.Position < input.Length )
				ProcessRevision(input, output, map, expected_rev);
		}

		private static void ProcessRevision(Stream input, Stream output, RevisionMap map, long expected_rev)
		{
			// make sure we're where we think we should be
			if( !PeekLine(input).StartsWith("Revision-number: ") )
				throw new ApplicationException("Revision expected, unknown data found");

			// get the revision headers
			Dictionary<string,string> headers = ReadHeaders(input);

			// mangle the revision number
			headers["Revision-number"] = Convert.ToString(expected_rev);

			// write the rev headers
			WriteHeaders(headers, output);

			// skip the revprop content
			TransferData(Convert.ToInt32(headers["Prop-content-length"]), input, output);

			// skip the newline after the revprops
			TransferLine(input, output);

			// process all the nodes
			while( input.Position < input.Length )
			{
				string line = PeekLine(input);

				// if we're on the next rev, exit
				if( line.StartsWith("Revision-number: ") )
					return;

				// make sure we got the node we were looking for
				if( !line.StartsWith("Node-path: ") )
					throw new ApplicationException("Unexpected content where we expected a node: " + line);

				// process the node
				ProcessNode(input, output, map);

				// if we're done, exit
				if( input.Position == input.Length )
					return;
			}

		}

		private static void ProcessNode(Stream input, Stream output, RevisionMap map)
		{
			// read the headers
			Dictionary<string,string> headers = ReadHeaders(input);

			// mangle the copyfrom-rev (maybe)
			if( headers.ContainsKey("Node-copyfrom-rev") )
			{
				long orig_rev = Convert.ToInt64(headers["Node-copyfrom-rev"]);
				long new_rev  = map.GetRevisionMap(orig_rev);

				Console.WriteLine("Changed copyfrom reference for {0} from {1} to {2}", headers["Node-path"], orig_rev, new_rev);

				headers["Node-copyfrom-rev"] = Convert.ToString(new_rev);
			}

			// write the headers
			WriteHeaders(headers, output);

			// skip the prop content (if any)
			if( headers.ContainsKey("Prop-content-length") )
				TransferData(Convert.ToInt32(headers["Prop-content-length"]), input, output);

			// skip the node text (if any)
			if( headers.ContainsKey("Text-content-length") )
				TransferData(Convert.ToInt32(headers["Text-content-length"]), input, output);

			// skip any newlines
			while( input.Position < input.Length && PeekLine(input) == "" )
				TransferLine(input, output);
		}

		private static Dictionary<string,string> ReadHeaders(Stream s)
		{
			Dictionary<string,string> ret = new Dictionary<string,string>();

			while( true )
			{
				// make sure we didn't hit the end
				if( s.Position == s.Length )
					throw new ApplicationException("end-of-stream reached while reading headers");

				// get the next line from the dumpfile
				string line = ReadLine(s);

				// if we read an empty line, we're done
				if( line == "" )
					return ret;

				// parse the key/value pair
				string key = line.Substring(0, line.IndexOf(':'));
				string val = line.Substring(line.IndexOf(' ') + 1);

				// add it to the headers
				ret.Add(key, val);
			}
		}

		private static void WriteHeaders(Dictionary<string,string> headers, Stream s)
		{
			foreach( KeyValuePair<string,string> entry in headers )
				WriteLine(string.Format("{0}: {1}", entry.Key, entry.Value), s);

			WriteLine("", s);
		}

		static string ReadLine(Stream s)
		{
			StringBuilder sb = new StringBuilder();

			while( true )
			{
				char c = (char)s.ReadByte();

				if( c == '\n' )
					return sb.ToString();
				else
					sb.Append(c);
			}
		}

		static string PeekLine(Stream s)
		{
			StringBuilder sb  = new StringBuilder();
			long          pos = s.Position;

			while( true )
			{
				char c = (char)s.ReadByte();

				if( c == '\n' )
				{
					s.Position = pos;
					return sb.ToString();
				}

				sb.Append(c);
			}
		}

		static void WriteLine(string line, Stream s)
		{
			if( !string.IsNullOrEmpty(line) )
			{
				byte[] b = Encoding.ASCII.GetBytes(line);

				s.Write(b, 0, b.Length);
			}

			s.WriteByte((byte)'\n');
		}

		static void TransferLine(Stream input, Stream output)
		{
			WriteLine(ReadLine(input), output);
		}

		static void TransferData(int count, Stream input, Stream output)
		{
			byte[] buffer = new byte[1024];

			while( count > 0 )
			{
				int to_read = Math.Min(buffer.Length, count);
				int read    = input.Read(buffer, 0, to_read);

				output.Write(buffer, 0, read);

				count -= read;
			}
		}

	}
}
