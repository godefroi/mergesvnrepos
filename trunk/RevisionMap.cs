using System;
using System.Collections.Generic;

namespace MergeSvnRepos
{
	class RevisionMap
	{
		Dictionary<long,long> m_mappings;

		public RevisionMap()
		{
			m_mappings = new Dictionary<long,long>();
		}

		public void AddRevisionMapping(long old_rev, long new_rev)
		{
			m_mappings.Add(old_rev, new_rev);
		}

		public long GetRevisionMap(long old_rev)
		{
			if( !m_mappings.ContainsKey(old_rev) )
				throw new ApplicationException("No revision mapping found for " + old_rev);

			return m_mappings[old_rev];
		}

	}
}
