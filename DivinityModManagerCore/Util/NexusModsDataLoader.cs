using DivinityModManager.Models;
using DivinityModManager.Models.NexusMods;

using NexusModsNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	public static class NexusModsDataLoader
	{
		private static INexusModsClient _client;

		public static void Init(string apiKey, string appName, string appVersion)
		{
			_client?.Dispose();
			_client = NexusModsClient.Create(apiKey, appName, appVersion);
		}

		public static bool LimitExceeded => _client == null || _client.RateLimitsManagement.ApiDailyLimitExceeded() || _client.RateLimitsManagement.ApiHourlyLimitExceeded();

		public static async Task<int> LoadAllModsDataAsync(List<DivinityModData> mods, CancellationToken t)
		{
			if (_client == null || LimitExceeded || mods.Count <= 0) return 0;
			var totalLoaded = 0;

			using (var dataLoader = new InfosInquirer(_client))
			{
				foreach (var mod in mods.Where(x => x.NexusModsData.ModId > -1))
				{
					var result = await dataLoader.Mods.GetMod(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, t);
					if(result != null)
					{
						mod.NexusModsData.Update(result);
						totalLoaded++;
					}

					if (t.IsCancellationRequested) break;
				}
			}

			return totalLoaded;
		}
	}
}
