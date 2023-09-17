using DivinityModManager.Models;
using DivinityModManager.Models.Cache;
using DivinityModManager.Util;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager.ModUpdater
{
	public class NexusModsCacheHandler : IExternalModCacheHandler<NexusModsCachedData>
	{
		public ModSourceType SourceType => ModSourceType.NEXUSMODS;
		public string FileName => "nexusmodsdata.json";
		public JsonSerializerSettings SerializerSettings => ModUpdateHandler.DefaultSerializerSettings;
		public bool IsEnabled { get; set; } = false;
		public NexusModsCachedData CacheData { get; set; }

		public string APIKey { get; set; }
		public string AppName { get; set; }
		public string AppVersion { get; set; }

		public NexusModsCacheHandler() : base()
		{
			CacheData = new NexusModsCachedData();
		}

		public async Task<bool> Update(IEnumerable<DivinityModData> mods, CancellationToken cts)
		{
			if (!NexusModsDataLoader.IsInitialized && !String.IsNullOrEmpty(APIKey))
			{
				NexusModsDataLoader.Init(APIKey, AppName, AppVersion);
			}

			if (NexusModsDataLoader.CanFetchData)
			{
				int successes = 0;
				try
				{
					successes = await NexusModsDataLoader.LoadAllModsDataAsync(mods, cts);
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error fetching NexusMods data:\n{ex}");
				}

				if (successes > 0)
				{
					DivinityApp.Log($"Fetched NexusMods mod info for {successes} mods.");

					foreach (var mod in mods.Where(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START).Select(x => x.NexusModsData))
					{
						CacheData.Mods[mod.UUID] = mod;
					}

					return true;
				}
				else
				{
					DivinityApp.Log($"Failed to fetch any NexusMods mod info.");
				}
			}
			else
			{
				DivinityApp.Log("NexusModsAPIKey not set, or daily/hourly limit reached. Skipping.");
			}
			return false;
		}
	}
}
