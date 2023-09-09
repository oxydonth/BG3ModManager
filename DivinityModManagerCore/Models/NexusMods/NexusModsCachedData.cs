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

		public List<NexusMod> Mods { get; set; }

		public NexusModsCachedData()
		{
			LastUpdated = -1;
			LastVersion = "";
		}
	}
}
