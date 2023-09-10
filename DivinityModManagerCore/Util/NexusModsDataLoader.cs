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
		private static bool _isActive = false;
		private static bool _pendingDispose = false;

		private static string _lastApiKey = "";

		public static void Init(string apiKey, string appName, string appVersion)
		{
			if(!String.IsNullOrEmpty(apiKey) && apiKey != _lastApiKey)
			{
				if (Dispose())
				{
					_lastApiKey = apiKey;
					_client = NexusModsClient.Create(apiKey, appName, appVersion);
				}
			}
		}

		public static bool Dispose()
		{
			if(!_isActive)
			{
				_client?.Dispose();
				_pendingDispose = false;
				return true;
			}
			_pendingDispose = true;
			return false;
		}

		public static bool CanFetchData => _client != null && !_client.RateLimitsManagement.ApiDailyLimitExceeded() && !_client.RateLimitsManagement.ApiHourlyLimitExceeded();
		public static bool LimitExceeded => _client != null && (_client.RateLimitsManagement.ApiDailyLimitExceeded() || !_client.RateLimitsManagement.ApiHourlyLimitExceeded());
		public static bool IsInitialized => _client != null;

		public static async Task<int> LoadAllModsDataAsync(List<DivinityModData> mods, CancellationToken t)
		{
			if (!CanFetchData || mods.Count <= 0) return 0;
			var totalLoaded = 0;

			_isActive = true;

			try
			{
				using (var dataLoader = new InfosInquirer(_client))
				{
					foreach (var mod in mods)
					{
						if (mod.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START)
						{
							var result = await dataLoader.Mods.GetMod(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, t);
							if (result != null)
							{
								mod.NexusModsData.Update(result);
								totalLoaded++;
							}
						}

						if (t.IsCancellationRequested) break;
					}
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Error fetching NexusMods data:\n{ex}");
			}

			_isActive = false;
			if(_pendingDispose) Dispose();

			return totalLoaded;
		}
	}
}
