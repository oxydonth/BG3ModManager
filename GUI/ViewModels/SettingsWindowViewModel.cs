using DivinityModManager.Models;
using DivinityModManager.Extensions;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using DynamicData;
using DynamicData.Aggregation;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using DivinityModManager.Models.Extender;
using System.Windows.Input;
using DivinityModManager.Util;
using System.IO;
using Newtonsoft.Json;
using DivinityModManager.Views;
using DivinityModManager.Models.App;
using System.Threading.Tasks;
using System.Reactive;
using System.Threading;
using DynamicData.Binding;
using System.ComponentModel;
using System.Reactive.Concurrency;
using DivinityModManager.Enums.Extender;

namespace DivinityModManager.ViewModels
{
	public enum SettingsWindowTab
	{
		[Description("Mod Manager Settings")]
		Default = 0,
		[Description("Script Extender Settings")]
		Extender = 1,
		[Description("Script Extender Updater Settings")]
		ExtenderUpdater = 2,
		[Description("Keybindings")]
		Keybindings = 3,
		[Description("Advanced Settings")]
		Advanced = 4
	}

	public class GameLaunchParamEntry
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public bool DebugModeOnly { get; set; }

		public GameLaunchParamEntry(string name, string description, bool debug = false)
		{
			Name = name;
			Description = description;
			DebugModeOnly = debug;
		}
	}

	public class SettingsWindowViewModel : ReactiveObject
	{
		private readonly MainWindowViewModel _main;
		public MainWindowViewModel Main => _main;

		public SettingsWindow View { get; private set; }

		public ObservableCollectionExtended<ScriptExtenderUpdateVersion> ScriptExtenderUpdates { get; private set; }
		[Reactive] public ScriptExtenderUpdateVersion TargetVersion { get; set; }
		public ObservableCollectionExtended<GameLaunchParamEntry> LaunchParams { get; private set; }

		[Reactive] public SettingsWindowTab SelectedTabIndex { get; set; }
		[Reactive] public Hotkey SelectedHotkey { get; set; }

		private readonly ObservableAsPropertyHelper<bool> _extenderTabIsVisible;
		public bool ExtenderTabIsVisible => _extenderTabIsVisible.Value;

		private readonly ObservableAsPropertyHelper<bool> _keybindingsTabIsVisible;
		public bool KeybindingsTabIsVisible => _keybindingsTabIsVisible.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _developerModeVisibility;
		public Visibility DeveloperModeVisibility => _developerModeVisibility.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _extenderTabVisibility;
		public Visibility ExtenderTabVisibility => _extenderTabVisibility.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _extenderUpdaterVisibility;
		public Visibility ExtenderUpdaterVisibility => _extenderUpdaterVisibility.Value;

		private readonly ObservableAsPropertyHelper<string> _resetSettingsCommandToolTip;
		public string ResetSettingsCommandToolTip => _resetSettingsCommandToolTip.Value;

		private Visibility BoolToVisibility(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
		private Visibility BoolToVisibility2(ValueTuple<bool, bool> b) => b.Item1 && b.Item2 ? Visibility.Visible : Visibility.Collapsed;

		public ICommand SaveSettingsCommand { get; private set; }
		public ICommand OpenSettingsFolderCommand { get; private set; }
		public ICommand ExportExtenderSettingsCommand { get; private set; }
		public ICommand ExportExtenderUpdaterSettingsCommand { get; private set; }
		public ICommand ResetSettingsCommand { get; private set; }
		public ICommand ClearCacheCommand { get; private set; }
		public ICommand AddLaunchParamCommand { get; private set; }
		public ICommand ClearLaunchParamsCommand { get; private set; }
		public ReactiveCommand<DependencyPropertyChangedEventArgs, Unit> OnWindowShownCommand { get; private set; }

		private readonly ScriptExtenderUpdateVersion _emptyVersion = new ScriptExtenderUpdateVersion();

		private string SelectedTabToResetTooltip(SettingsWindowTab tab)
		{
			var name = TabToName(tab);
			return $"Reset {name}";
		}

		private string TabToName(SettingsWindowTab tab) => tab.GetDescription();

		public async Task<Unit> GetExtenderUpdatesAsync(ExtenderUpdateChannel channel = ExtenderUpdateChannel.Release)
		{
			var url = String.Format(DivinityApp.EXTENDER_MANIFESTS_URL, channel.GetDescription());
			DivinityApp.Log($"Checking for script extender manifest info at '{url}'");
			var text = await WebHelper.DownloadUrlAsStringAsync(url);
//#if DEBUG
//			DivinityApp.Log($"Manifest info:\n{text}");
//#endif
			if (!String.IsNullOrEmpty(text))
			{
				if(DivinityJsonUtils.TrySafeDeserialize<ScriptExtenderUpdateData>(text, out var data))
				{
					var res = data.Resources.FirstOrDefault();
					if(res != null)
					{
						var lastVersion = TargetVersion != _emptyVersion ? TargetVersion?.BuildDate : null;
						await Observable.Start(() =>
						{
							ScriptExtenderUpdates.Clear();
							ScriptExtenderUpdates.Add(_emptyVersion);
							ScriptExtenderUpdates.AddRange(res.Versions.OrderByDescending(x => x.BuildDate));
							if(lastVersion != null)
							{
								TargetVersion = ScriptExtenderUpdates.FirstOrDefault(x => x.BuildDate == lastVersion) ?? ScriptExtenderUpdates.LastOrDefault();
							}
							else
							{
								TargetVersion = _emptyVersion;
								//TargetVersion = ScriptExtenderUpdates.LastOrDefault();
							}
						}, RxApp.MainThreadScheduler);
					}
				}
			}
			return Unit.Default;
		}

		private IDisposable _manifestFetchingTask;
		private long _lastManifestCheck = -1;

		private bool CanCheckManifest => _lastManifestCheck == -1 || DateTimeOffset.Now.ToUnixTimeSeconds() - _lastManifestCheck >= 3000;

		private void FetchLatestManifestData(ExtenderUpdateChannel channel, bool force = false)
		{
			if (force || CanCheckManifest)
			{
				_manifestFetchingTask?.Dispose();

				_lastManifestCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
				_manifestFetchingTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, cts) => await GetExtenderUpdatesAsync(channel));
			}
		}

		private void OnWindowVisibilityChanged(DependencyPropertyChangedEventArgs e)
		{
			_manifestFetchingTask?.Dispose();

			if ((bool)e.NewValue == true)
			{
				_manifestFetchingTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromMilliseconds(100), async (sch, cts) => await GetExtenderUpdatesAsync(ExtenderUpdaterSettings.UpdateChannel));
				//FetchLatestManifestData(ExtenderUpdaterSettings.UpdateChannel);
			}
		}

		public DivinityModManagerSettings Settings { get; private set; }
		public ScriptExtenderSettings ExtenderSettings { get; private set; }
		public ScriptExtenderUpdateConfig ExtenderUpdaterSettings { get; private set; }

		public void OnTargetVersionSelected(ScriptExtenderUpdateVersion entry)
		{
			if (entry != _emptyVersion)
			{
				ExtenderUpdaterSettings.TargetVersion = entry.Version;
				ExtenderUpdaterSettings.TargetResourceDigest = entry.Digest;
			}
			else
			{
				ExtenderUpdaterSettings.TargetVersion = "";
				ExtenderUpdaterSettings.TargetResourceDigest = "";
			}
		}

		public void OnTargetVersionSelected(object entry)
		{
			OnTargetVersionSelected((ScriptExtenderUpdateVersion)entry);
		}

		public SettingsWindowViewModel(SettingsWindow view, MainWindowViewModel main)
		{
			_main = main;
			View = view;

			TargetVersion = _emptyVersion;

			Main.WhenAnyValue(x => x.Settings).BindTo(this, x => x.Settings);
			Main.WhenAnyValue(x => x.Settings.ExtenderSettings).BindTo(this, x => x.ExtenderSettings);
			Main.WhenAnyValue(x => x.Settings.ExtenderUpdaterSettings).BindTo(this, x => x.ExtenderUpdaterSettings);

			ScriptExtenderUpdates = new ObservableCollectionExtended<ScriptExtenderUpdateVersion>() { _emptyVersion };
			LaunchParams = new ObservableCollectionExtended<GameLaunchParamEntry>()
			{
				new GameLaunchParamEntry("-continueGame", "Automatically load the last save when loading into the main menu"),
				new GameLaunchParamEntry("-storylog 1", "Enables the story log"),
				new GameLaunchParamEntry(@"--logPath """, "A directory to write story logs to"),
				new GameLaunchParamEntry("--cpuLimit x", "Limit the cpu to x amount of threads (unknown if this works)"),
				new GameLaunchParamEntry("-asserts 1", "", true),
				new GameLaunchParamEntry("-stats 1", "", true),
				new GameLaunchParamEntry("-dynamicStory 1", "", true),
				new GameLaunchParamEntry("-externalcrashhandler", "", true),
				new GameLaunchParamEntry(@"-nametag """, "", true),
				new GameLaunchParamEntry(@"-module """, "", true),
				new GameLaunchParamEntry(@"+connect_lobby """, "", true),
				new GameLaunchParamEntry("-locaupdater 1", "", true),
				new GameLaunchParamEntry(@"-mediaPath """, "", true),
			};

			var whenTab = this.WhenAnyValue(x => x.SelectedTabIndex);
			whenTab.Select(x => x == SettingsWindowTab.Extender).ToProperty(this, nameof(ExtenderTabIsVisible));
			whenTab.Select(x => x == SettingsWindowTab.Keybindings).ToProperty(this, nameof(KeybindingsTabIsVisible));

			this.WhenAnyValue(x => x.Settings.SkipLauncher, x => x.KeybindingsTabIsVisible);
			this.WhenAnyValue(x => x.TargetVersion).Skip(1).WhereNotNull().ObserveOn(RxApp.MainThreadScheduler).Subscribe(OnTargetVersionSelected);

			_resetSettingsCommandToolTip = this.WhenAnyValue(x => x.SelectedTabIndex).Select(SelectedTabToResetTooltip).ToProperty(this, nameof(ResetSettingsCommandToolTip), scheduler: RxApp.MainThreadScheduler);

			_developerModeVisibility = Settings.WhenAnyValue(x => x.DebugModeEnabled).Select(BoolToVisibility).ToProperty(this, nameof(DeveloperModeVisibility), scheduler: RxApp.MainThreadScheduler);

			_extenderTabVisibility = this.WhenAnyValue(x => x.ExtenderSettings.ExtenderUpdaterIsAvailable)
				.Select(BoolToVisibility).ToProperty(this, nameof(ExtenderTabVisibility), true, RxApp.MainThreadScheduler);

			_extenderUpdaterVisibility = this.WhenAnyValue(x => x.ExtenderSettings.ExtenderUpdaterIsAvailable, x => x.Settings.DebugModeEnabled)
				.Select(BoolToVisibility2).ToProperty(this, nameof(ExtenderUpdaterVisibility), true, RxApp.MainThreadScheduler);

			ExtenderUpdaterSettings.WhenAnyValue(x => x.UpdateChannel).Subscribe((channel) =>
			{
				if(View.IsVisible)
				{
					FetchLatestManifestData(channel, true);
				}
			});

			SaveSettingsCommand = ReactiveCommand.Create(() =>
			{
				var settingsFile = DivinityApp.GetAppDirectory("Data", "settings.json");
				try
				{

					System.IO.FileAttributes attr = File.GetAttributes(Settings.GameExecutablePath);

					if (attr.HasFlag(System.IO.FileAttributes.Directory))
					{
						string exeName = "";
						if (!DivinityRegistryHelper.IsGOG)
						{
							exeName = Path.GetFileName(Main.AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exeName = Path.GetFileName(Main.AppSettings.DefaultPathways.GOG.ExePath);
						}

						var exe = Path.Combine(Settings.GameExecutablePath, exeName);
						if (File.Exists(exe))
						{
							Settings.GameExecutablePath = exe;
						}
					}

					// Help for people confused about needing to click the export button to save the json
					if (SelectedTabIndex == SettingsWindowTab.Extender)
					{
						ExportExtenderSettingsCommand?.Execute(null);
					}
					else if (SelectedTabIndex == SettingsWindowTab.ExtenderUpdater)
					{
						ExportExtenderUpdaterSettingsCommand?.Execute(null);
					}
				}
				catch (Exception) { }
				if (Main.SaveSettings())
				{
					Main.ShowAlert($"Saved settings to '{settingsFile}'", AlertType.Success, 10);
				}
			});

			OpenSettingsFolderCommand = ReactiveCommand.Create(() =>
			{
				DivinityFileUtils.TryOpenPath(DivinityApp.GetAppDirectory(DivinityApp.DIR_DATA));
			});

			var _jsonConfigExportSettings = new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented
			};

			ExportExtenderSettingsCommand = ReactiveCommand.Create(() =>
			{
				string outputFile = Path.Combine(Path.GetDirectoryName(Settings.GameExecutablePath), "ScriptExtenderSettings.json");
				try
				{
					_jsonConfigExportSettings.DefaultValueHandling = ExtenderSettings.ExportDefaultExtenderSettings ? DefaultValueHandling.Include : DefaultValueHandling.Ignore;
					string contents = JsonConvert.SerializeObject(Settings.ExtenderSettings, _jsonConfigExportSettings);
					File.WriteAllText(outputFile, contents);
					Main.ShowAlert($"Saved Script Extender settings to '{outputFile}'", AlertType.Success, 20);
				}
				catch (Exception ex)
				{
					Main.ShowAlert($"Error saving Script Extender settings to '{outputFile}':\n{ex}", AlertType.Danger);
				}
			});

			ExportExtenderUpdaterSettingsCommand = ReactiveCommand.Create(() =>
			{
				string outputFile = Path.Combine(Path.GetDirectoryName(Settings.GameExecutablePath), "ScriptExtenderUpdaterConfig.json");
				try
				{
					_jsonConfigExportSettings.DefaultValueHandling = ExtenderSettings.ExportDefaultExtenderSettings ? DefaultValueHandling.Include : DefaultValueHandling.Ignore;
					string contents = JsonConvert.SerializeObject(Settings.ExtenderUpdaterSettings, _jsonConfigExportSettings);
					File.WriteAllText(outputFile, contents);
					Main.ShowAlert($"Saved Script Extender Updater settings to '{outputFile}'", AlertType.Success, 20);
				}
				catch (Exception ex)
				{
					Main.ShowAlert($"Error saving Script Extender Updater settings to '{outputFile}':\n{ex}", AlertType.Danger);
				}
			});

			ResetSettingsCommand = ReactiveCommand.Create(() =>
			{
				var tabName = TabToName(SelectedTabIndex);
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View, $"Reset {tabName} to Default?\nCurrent settings will be lost.", $"Confirm {tabName} Reset",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Main.View.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					switch(SelectedTabIndex)
					{
						case SettingsWindowTab.Default:
							Settings.SetToDefault();
							break;
						case SettingsWindowTab.Extender:
							Settings.ExtenderSettings.SetToDefault();
							break;
						case SettingsWindowTab.ExtenderUpdater:
							Settings.ExtenderUpdaterSettings.SetToDefault();
							break;
						case SettingsWindowTab.Keybindings:
							Main.Keys.SetToDefault();
							break;
					}
				}
			});

			ClearCacheCommand = ReactiveCommand.Create(() =>
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View, $"Delete local mod cache?\nThis cannot be undone.", "Confirm Delete Cache",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Main.View.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					try
					{
						if (Main.UpdateHandler.DeleteCache())
						{
							Main.ShowAlert($"Deleted local cache in {DivinityApp.GetAppDirectory("Data")}", AlertType.Success, 20);
						}
						else
						{
							Main.ShowAlert($"No cache to delete.", AlertType.Warning, 20);
						}
					}
					catch (Exception ex)
					{
						Main.ShowAlert($"Error deleting workshop cache:\n{ex}", AlertType.Danger);
					}
				}
			});

			AddLaunchParamCommand = ReactiveCommand.Create((string param) =>
			{
				if (Settings.GameLaunchParams == null) Settings.GameLaunchParams = "";
				if (Settings.GameLaunchParams.IndexOf(param) < 0)
				{
					if (String.IsNullOrWhiteSpace(Settings.GameLaunchParams))
					{
						Settings.GameLaunchParams = param;
					}
					else
					{
						Settings.GameLaunchParams = Settings.GameLaunchParams + " " + param;
					}
				}
			});

			ClearLaunchParamsCommand = ReactiveCommand.Create(() =>
			{
				Settings.GameLaunchParams = "";
			});

			OnWindowShownCommand = ReactiveCommand.Create<DependencyPropertyChangedEventArgs>(OnWindowVisibilityChanged);
		}
	}
}
