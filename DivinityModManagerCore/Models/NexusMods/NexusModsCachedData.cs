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

		public Dictionary<string,NexusModsModData> Mods { get; set; } = new Dictionary<string, NexusModsModData>();

		public NexusModsCachedData()
		{
			LastUpdated = -1;
			LastVersion = "";
		}
	}
}
