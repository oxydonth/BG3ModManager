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
		Keybindings = 3
	}

	public class SettingsWindowViewModel : ReactiveObject
	{
		public MainWindowViewModel Main { get; set; }
		public SettingsWindow View { get; private set; }

		[Reactive] public DivinityModManagerSettings Settings { get; set; }

		public ObservableCollectionExtended<ScriptExtenderUpdateVersion> ScriptExtenderUpdates { get; private set; }
		[Reactive] public ScriptExtenderUpdateVersion TargetVersion { get; set; }

		[Reactive] public bool ExtenderTabIsVisible { get; set; }
		[Reactive] public bool KeybindingsTabIsVisible { get; set; }
		[Reactive] public SettingsWindowTab SelectedTabIndex { get; set; }
		[Reactive] public Hotkey SelectedHotkey { get; set; }

		private readonly ObservableAsPropertyHelper<Visibility> _developerModeVisibility;
		public Visibility DeveloperModeVisibility => _developerModeVisibility.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _extenderTabVisibility;
		public Visibility ExtenderTabVisibility => _extenderTabVisibility.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _extenderUpdaterVisibility;
		public Visibility ExtenderUpdaterVisibility => _extenderUpdaterVisibility.Value;

		private readonly ObservableAsPropertyHelper<ScriptExtenderSettings> _extenderSettings;
		public ScriptExtenderSettings ExtenderSettings => _extenderSettings.Value;

		private readonly ObservableAsPropertyHelper<ScriptExtenderUpdateConfig> _extenderUpdaterSettings;
		public ScriptExtenderUpdateConfig ExtenderUpdaterSettings => _extenderUpdaterSettings.Value;

		private readonly ObservableAsPropertyHelper<string> _resetSettingsCommandToolTip;
		public string ResetSettingsCommandToolTip => _resetSettingsCommandToolTip.Value;

		private Visibility BoolToVisibility(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
		private Visibility BoolToVisibility2(ValueTuple<bool, bool> b) => b.Item1 && b.Item2 ? Visibility.Visible : Visibility.Collapsed;

		public ICommand SaveSettingsCommand { get; private set; }
		public ICommand OpenSettingsFolderCommand { get; private set; }
		public ICommand ExportExtenderSettingsCommand { get; private set; }
		public ICommand ExportExtenderUpdaterSettingsCommand { get; private set; }
		public ICommand ResetSettingsCommand { get; private set; }
		public ICommand ResetKeybindingsCommand { get; private set; }
		public ICommand ClearCacheCommand { get; private set; }
		public ICommand AddLaunchParamCommand { get; private set; }
		public ICommand ClearLaunchParamsCommand { get; private set; }

		private string SelectedTabToResetTooltip(SettingsWindowTab tab)
		{
			var name = TabToName(tab);
			return $"Reset {name}";
		}

		private string TabToName(SettingsWindowTab tab) => tab.GetDescription();

		public async Task<Unit> GetExtenderUpdatesAsync(string channel = "Release")
		{
			var url = String.Format(DivinityApp.EXTENDER_MANIFESTS_URL, channel);
			var text = await WebHelper.DownloadUrlAsStringAsync(url);
			if (!String.IsNullOrEmpty(text))
			{
				if(DivinityJsonUtils.TrySafeDeserialize<ScriptExtenderUpdateData>(text, out var data))
				{
					var res = data.Resources.FirstOrDefault();
					if(res != null)
					{
						ScriptExtenderUpdates.Clear();
						ScriptExtenderUpdates.AddRange(res.Versions);
					}
				}
			}
			return Unit.Default;
		}

		public SettingsWindowViewModel(SettingsWindow view)
		{
			View = view;
			SelectedTabIndex = 0;

			ScriptExtenderUpdates = new ObservableCollectionExtended<ScriptExtenderUpdateVersion>();

			this.WhenAnyValue(x => x.SelectedTabIndex, (index) => index == SettingsWindowTab.Extender).BindTo(this, x => x.ExtenderTabIsVisible);
			this.WhenAnyValue(x => x.SelectedTabIndex, (index) => index == SettingsWindowTab.Keybindings).BindTo(this, x => x.KeybindingsTabIsVisible);

			this.WhenAnyValue(x => x.Settings.SkipLauncher, x => x.KeybindingsTabIsVisible);
			this.WhenAnyValue(x => x.TargetVersion).Select(x => x.Version).BindTo(this, x => x.ExtenderUpdaterSettings.TargetVersion);

			_resetSettingsCommandToolTip = this.WhenAnyValue(x => x.SelectedTabIndex).Select(SelectedTabToResetTooltip).ToProperty(this, nameof(ResetSettingsCommandToolTip), scheduler: RxApp.MainThreadScheduler);

			_developerModeVisibility = this.WhenAnyValue(x => x.Settings.DebugModeEnabled).Select(BoolToVisibility).ToProperty(this, nameof(DeveloperModeVisibility), scheduler: RxApp.MainThreadScheduler);

			_extenderSettings = this.WhenAnyValue(x => x.Settings.ExtenderSettings).ToProperty(this, nameof(ExtenderSettings));
			_extenderUpdaterSettings = this.WhenAnyValue(x => x.Settings.ExtenderUpdaterSettings).ToProperty(this, nameof(ExtenderUpdaterSettings));

			_extenderTabVisibility = this.WhenAnyValue(x => x.ExtenderSettings.ExtenderUpdaterIsAvailable)
				.Select(BoolToVisibility).ToProperty(this, nameof(ExtenderTabVisibility), scheduler: RxApp.MainThreadScheduler);

			_extenderUpdaterVisibility = this.WhenAnyValue(x => x.ExtenderSettings.ExtenderUpdaterIsAvailable, x => x.Settings.DebugModeEnabled)
				.Select(BoolToVisibility2).ToProperty(this, nameof(ExtenderUpdaterVisibility), scheduler:RxApp.MainThreadScheduler);


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

			var canResetExtenderSettingsObservable = this.WhenAny(x => x.Settings.ExtenderSettings, (extenderSettings) => extenderSettings != null).StartWith(false);
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
			}, canResetExtenderSettingsObservable);

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
		}
	}
}
