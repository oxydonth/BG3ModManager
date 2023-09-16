using DivinityModManager.Models;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager.ModUpdater
{
	public class ModUpdateHandler
	{
		public NexusModsCacheHandler Nexus { get; private set; }
		public SteamWorkshopCacheHandler Workshop { get; private set; }
		public GithubModsCacheHandler Github { get; private set; }

		public static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			Formatting = Formatting.None
		};

		public async Task<bool> UpdateAsync(IEnumerable<DivinityModData> mods, CancellationToken cts)
		{
			if(Workshop.IsEnabled)
			{
				await Workshop.Update(mods, cts);
			}
			if(Nexus.IsEnabled)
			{
				await Nexus.Update(mods, cts);
			}
			if(Github.IsEnabled)
			{
				await Github.Update(mods, cts);
			}
			return false;
		}

		public async Task<bool> LoadAsync(string currentAppVersion, CancellationToken cts)
		{
			if(Workshop.IsEnabled)
			{
				if((DateTimeOffset.Now.ToUnixTimeSeconds() - Workshop.CacheData.LastUpdated >= 3600))
				{
					await Workshop.LoadCacheAsync(currentAppVersion, cts);
				}
			}
			if(Nexus.IsEnabled)
			{
				var data = await Nexus.LoadCacheAsync(currentAppVersion, cts);
				foreach (var entry in data.Mods)
				{
					if (Nexus.CacheData.Mods.TryGetValue(entry.Key, out var existing))
					{
						if (existing.UpdatedTimestamp < entry.Value.UpdatedTimestamp || !existing.IsUpdated)
						{
							Nexus.CacheData.Mods[entry.Key] = entry.Value;
						}
					}
					else
					{
						Nexus.CacheData.Mods[entry.Key] = entry.Value;
					}
				}
			}
			if(Github.IsEnabled)
			{
				await Github.LoadCacheAsync(currentAppVersion, cts);
			}
			return false;
		}

		public async Task<bool> SaveAsync(IEnumerable<DivinityModData> mods, string currentAppVersion, CancellationToken cts)
		{
			if(Workshop.IsEnabled)
			{
				await Workshop.SaveCacheAsync(true, currentAppVersion, cts);
			}
			if(Nexus.IsEnabled)
			{
				foreach (var mod in mods.Where(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START).Select(x => x.NexusModsData))
				{
					Nexus.CacheData.Mods[mod.UUID] = mod;
				}
				await Nexus.SaveCacheAsync(true, currentAppVersion, cts);
			}
			if(Github.IsEnabled)
			{
				await Github.SaveCacheAsync(true, currentAppVersion, cts);
			}
			return false;
		}

		public ModUpdateHandler()
		{
			Nexus = new NexusModsCacheHandler();
			Workshop = new SteamWorkshopCacheHandler();
			Github = new GithubModsCacheHandler();
		}
	}
}
