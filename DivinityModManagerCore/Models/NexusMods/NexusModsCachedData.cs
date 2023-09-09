using DynamicData;

using NexusModsNET.DataModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.NexusMods
{
	public class NexusModsCachedData
	{
		public long LastUpdated { get; set; }
		public string LastVersion { get; set; }

		public SourceCache<NexusMod, long> Mods { get; set; } = new SourceCache<NexusMod, long>(x => x.ModId);

		public NexusModsCachedData()
		{
			LastUpdated = -1;
			LastVersion = "";
		}
	}
}
