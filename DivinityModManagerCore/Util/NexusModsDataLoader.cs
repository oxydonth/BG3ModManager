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

		private static bool LimitExceededCheck()
		{
			if (_client != null)
			{
				var daily = _client.RateLimitsManagement.ApiDailyLimitExceeded();
				var hourly = _client.RateLimitsManagement.ApiHourlyLimitExceeded();

				if(daily)
				{
					DivinityApp.Log($"Daily limit exceeded ({_client.RateLimitsManagement.APILimits.DailyLimit})");
					return true;
				}
				else if(hourly)
				{
					DivinityApp.Log($"Hourly limit exceeded ({_client.RateLimitsManagement.APILimits.HourlyLimit})");
					return true;
				}
			}
			return false;
		}

		public static bool CanDoTask(int apiCalls)
		{
			if(_client != null)
			{
				var currentLimit = Math.Min(_client.RateLimitsManagement.APILimits.HourlyRemaining, _client.RateLimitsManagement.APILimits.DailyRemaining);
				if (currentLimit > apiCalls)
				{
					return true;
				}
			}
			return false;
		}

		private static void OnTaskDone()
		{
			_isActive = false;
			if (_pendingDispose) Dispose();
		}

		public static async Task<List<NexusModsModDownloadLink>> GetLatestDownloadsForMods(List<DivinityModData> mods, CancellationToken t)
		{
			var links = new List<NexusModsModDownloadLink>();
			if (!CanFetchData || mods.Count <= 0) return links;
			_isActive = true;

			try
			{
				var apiCallAmount = mods.Count(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START) & 2;
				if(!CanDoTask(apiCallAmount))
				{
					var apiAmounts = _client.RateLimitsManagement.APILimits;

					DivinityApp.Log($"Task would exceed hourly or daily API limits. ExpectedCalls({apiCallAmount}) HourlyRemaining({apiAmounts.HourlyRemaining}/{apiAmounts.HourlyLimit}) DailyRemaining({apiAmounts.DailyRemaining}/{apiAmounts.DailyLimit})");
					OnTaskDone();
					return links;
				}
				using (var dataLoader = new InfosInquirer(_client))
				{
					foreach (var mod in mods)
					{
						if (mod.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START)
						{
							var result = await dataLoader.ModFiles.GetModFilesAsync(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, t);
							if (result != null)
							{
								var file = result.ModFiles.FirstOrDefault(x => x.IsPrimary);
								if(file != null)
								{
									var fileId = file.FileId;
									var linkResult = await dataLoader.ModFiles.GetModFileDownloadLinksAsync(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, fileId, t);
									if(linkResult != null && linkResult.Count() > 0)
									{
										var primaryLink = linkResult.FirstOrDefault();
										links.Add(new NexusModsModDownloadLink(mod, primaryLink));
									}
								}
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

			OnTaskDone();

			return links;
		}

		public static async Task<int> LoadAllModsDataAsync(List<DivinityModData> mods, CancellationToken t)
		{
			if (!CanFetchData || mods.Count <= 0) return 0;
			var totalLoaded = 0;

			_isActive = true;

			try
			{
				var apiCallAmount = mods.Count(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START);
				if (!CanDoTask(apiCallAmount))
				{
					var apiAmounts = _client.RateLimitsManagement.APILimits;

					DivinityApp.Log($"Task would exceed hourly or daily API limits. ExpectedCalls({apiCallAmount}) HourlyRemaining({apiAmounts.HourlyRemaining}/{apiAmounts.HourlyLimit}) DailyRemaining({apiAmounts.DailyRemaining}/{apiAmounts.DailyLimit})");
					OnTaskDone();
					return totalLoaded;
				}

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

			OnTaskDone();

			return totalLoaded;
		}
	}
}
