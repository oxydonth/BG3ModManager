using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Util;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Aggregation;

using LSLib.LS;

using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Reflection;

namespace DivinityModManager.Models
{
	[DataContract]
	[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
	public class DivinityModData : DivinityBaseModData, ISelectable
	{
		[Reactive][DataMember] public int Index { get; set; }

		[DataMember(Name = "FileName")]
		public string OutputPakName
		{
			get
			{
				if (!Folder.Contains(UUID))
				{
					return Path.ChangeExtension($"{Folder}_{UUID}", "pak");
				}
				else
				{
					return Path.ChangeExtension($"{FileName}", "pak");
				}
			}
		}

		[Reactive][DataMember(Name = "Type")] public string ModType { get; set; }

		[DataMember] public List<string> Modes { get; set; } = new List<string>();

		[DataMember] public string Targets { get; set; }
		[Reactive] public DateTime? LastUpdated { get; set; }

		private DivinityExtenderModStatus extenderModStatus = DivinityExtenderModStatus.NONE;

		public static int CurrentExtenderVersion { get; set; } = -1;

		public DivinityExtenderModStatus ExtenderModStatus
		{
			get => extenderModStatus;
			set
			{
				this.RaiseAndSetIfChanged(ref extenderModStatus, value);
				UpdateScriptExtenderToolTip(CurrentExtenderVersion);
			}
		}

		public string ScriptExtenderSupportToolTipText { get; private set; }

		public void UpdateScriptExtenderToolTip(int currentVersion = -1)
		{
			switch (ExtenderModStatus)
			{
				case DivinityExtenderModStatus.REQUIRED:
				case DivinityExtenderModStatus.REQUIRED_MISSING:
				case DivinityExtenderModStatus.REQUIRED_DISABLED:
				case DivinityExtenderModStatus.REQUIRED_OLD:
					ScriptExtenderSupportToolTipText = "";
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
					{
						ScriptExtenderSupportToolTipText = "[MISSING] ";
					}
					else if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED)
					{
						ScriptExtenderSupportToolTipText = "[EXTENSIONS DISABLED] ";
					}
					else if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_OLD)
					{
						ScriptExtenderSupportToolTipText = "[OLD] ";
					}
					if (ScriptExtenderData.RequiredExtensionVersion > -1)
					{
						ScriptExtenderSupportToolTipText += $"Requires Script Extender v{ScriptExtenderData.RequiredExtensionVersion} or higher";
					}
					else
					{
						ScriptExtenderSupportToolTipText += "Requires the Script Extender";
					}
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED)
					{
						ScriptExtenderSupportToolTipText += " (Enable Extensions in the Script Extender config)";
					}
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_OLD)
					{
						ScriptExtenderSupportToolTipText += " (Update by running the game)";
					}
					break;
				case DivinityExtenderModStatus.SUPPORTS:
					if (ScriptExtenderData.RequiredExtensionVersion > -1)
					{
						ScriptExtenderSupportToolTipText = $"Supports Script Extender v{ScriptExtenderData.RequiredExtensionVersion} or higher";
					}
					else
					{
						ScriptExtenderSupportToolTipText = "Supports the Script Extender";
					}
					break;
				case DivinityExtenderModStatus.NONE:
				default:
					ScriptExtenderSupportToolTipText = "";
					break;
			}
			if (ScriptExtenderSupportToolTipText != "")
			{
				ScriptExtenderSupportToolTipText += Environment.NewLine;
			}
			if (currentVersion > -1)
			{
				ScriptExtenderSupportToolTipText += $"(Currently installed version is v{currentVersion})";
			}
			else
			{
				ScriptExtenderSupportToolTipText += "(No installed extender version found)";
			}
			this.RaisePropertyChanged("ScriptExtenderSupportToolTipText");
		}

		[DataMember] public DivinityModScriptExtenderConfig ScriptExtenderData { get; set; }
		[DataMember] public SourceList<DivinityModDependencyData> Dependencies { get; set; } = new SourceList<DivinityModDependencyData>();

		protected ReadOnlyObservableCollection<DivinityModDependencyData> displayedDependencies;
		public ReadOnlyObservableCollection<DivinityModDependencyData> DisplayedDependencies => displayedDependencies;

		public override string GetDisplayName()
		{
			if (DisplayFileForName)
			{
				if (!IsEditorMod)
				{
					return FileName;
				}
				else
				{
					return Folder + " [Editor Project]";
				}
			}
			else
			{
				if (!DivinityApp.DeveloperModeEnabled && UUID == DivinityApp.MAIN_CAMPAIGN_UUID)
				{
					return "Main";
				}
				return Name;
			}
		}

		private readonly ObservableAsPropertyHelper<bool> hasToolTip;

		public bool HasToolTip => hasToolTip.Value;

		private readonly ObservableAsPropertyHelper<int> dependencyCount;
		public int TotalDependencies => dependencyCount.Value;

		private readonly ObservableAsPropertyHelper<bool> hasDependencies;

		public bool HasDependencies => hasDependencies.Value;

		private readonly ObservableAsPropertyHelper<Visibility> dependencyVisibility;
		public Visibility DependencyVisibility => dependencyVisibility.Value;

		[Reactive] public bool HasScriptExtenderSettings { get; set; }

		[Reactive] public bool IsEditorMod { get; set; }

		[Reactive] public bool IsActive { get; set; }

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				if (value && Visibility != Visibility.Visible)
				{
					value = false;
				}
				this.RaiseAndSetIfChanged(ref isSelected, value);
			}
		}

		private readonly ObservableAsPropertyHelper<bool> _canDelete;
		public bool CanDelete => _canDelete.Value;

		private readonly ObservableAsPropertyHelper<bool> _canAddToLoadOrder;
		public bool CanAddToLoadOrder => _canAddToLoadOrder.Value;

		private readonly ObservableAsPropertyHelper<bool> _canOpenWorkshopLink;
		public bool CanOpenWorkshopLink => _canOpenWorkshopLink.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _openWorkshopLinkVisibility;
		public Visibility OpenWorkshopLinkVisibility => _openWorkshopLinkVisibility.Value;

		private readonly ObservableAsPropertyHelper<Visibility> _toggleForceAllowInLoadOrderVisibility;
		public Visibility ToggleForceAllowInLoadOrderVisibility => _toggleForceAllowInLoadOrderVisibility.Value;

		[Reactive] public bool CanDrag { get; set; }

		[Reactive] public bool DeveloperMode { get; set; }

		[Reactive] public bool HasColorOverride { get; set; }
		[Reactive] public string SelectedColor { get; set; }
		[Reactive] public string ListColor { get; set; }

		private DivinityModWorkshopData workshopData = new DivinityModWorkshopData();

		public DivinityModWorkshopData WorkshopData
		{
			get => workshopData;
			set { this.RaiseAndSetIfChanged(ref workshopData, value); }
		}

		//public DivinityModWorkshopData WorkshopData { get; private set; } = new DivinityModWorkshopData();
		public ICommand OpenWorkshopPageCommand { get; private set; }
		public ICommand OpenWorkshopPageInSteamCommand { get; private set; }

		public string GetURL(bool asSteamBrowserProtocol = false)
		{
			if (WorkshopData != null && WorkshopData.ID != "")
			{
				if (!asSteamBrowserProtocol)
				{
					return $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopData.ID}";
				}
				else
				{
					return $"steam://url/CommunityFilePage/{WorkshopData.ID}";
				}
			}
			return "";
		}

		public override string ToString()
		{
			return $"Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID})";
		}

		public DivinityLoadOrderEntry ToOrderEntry()
		{
			return new DivinityLoadOrderEntry
			{
				UUID = this.UUID,
				Name = this.Name
			};
		}

		public DivinityProfileActiveModData ToProfileModData()
		{
			return new DivinityProfileActiveModData()
			{
				Folder = Folder,
				MD5 = MD5,
				Name = Name,
				UUID = UUID,
				Version = Version.VersionInt
			};
		}

		public void AllowInLoadOrder(bool b)
		{
			ForceAllowInLoadOrder = b;
			IsActive = b && IsForceLoaded;
		}

		public DivinityModData(bool isBaseGameMod = false) : base()
		{
			Targets = "";
			Index = -1;
			CanDrag = true;

			_toggleForceAllowInLoadOrderVisibility = this.WhenAnyValue(x => x.IsForceLoaded, x => x.HasMetadata)
				.Select(b => b.Item1 && b.Item2 ? Visibility.Visible : Visibility.Collapsed)
				.StartWith(Visibility.Collapsed)
				.ToProperty(this, nameof(ToggleForceAllowInLoadOrderVisibility), scheduler: RxApp.MainThreadScheduler);

			_canOpenWorkshopLink = this.WhenAnyValue(x => x.IsHidden, x => x.IsLarianMod, x => x.WorkshopData.ID,
	(b1, b2, id) => !b1 & !b2 & !String.IsNullOrEmpty(id)).ToProperty(this, nameof(CanOpenWorkshopLink));

			_openWorkshopLinkVisibility = this.WhenAnyValue(x => x.CanOpenWorkshopLink)
				.Select(b => b ? Visibility.Visible : Visibility.Collapsed)
				.StartWith(Visibility.Collapsed)
				.ToProperty(this, nameof(OpenWorkshopLinkVisibility), scheduler: RxApp.MainThreadScheduler);

			var connection = this.Dependencies.Connect().ObserveOn(RxApp.MainThreadScheduler);
			connection.Bind(out displayedDependencies).DisposeMany().Subscribe();
			dependencyCount = connection.Count().StartWith(0).ToProperty(this, nameof(TotalDependencies));
			hasDependencies = this.WhenAnyValue(x => x.TotalDependencies, c => c > 0).StartWith(false).ToProperty(this, nameof(HasDependencies));
			dependencyVisibility = this.WhenAnyValue(x => x.HasDependencies, b => b ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Collapsed).ToProperty(this, nameof(DependencyVisibility));
			this.WhenAnyValue(x => x.IsActive, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod,
				x => x.ForceAllowInLoadOrder).Subscribe((b) =>
			{
				var isActive = b.Item1;
				var isForceLoaded = b.Item2;
				var isForceLoadedMergedMod = b.Item3;
				var forceAllowInLoadOrder = b.Item4;

				if(forceAllowInLoadOrder || isActive)
				{
					CanDrag = true;
				}
				else
				{
					CanDrag = !isForceLoaded || isForceLoadedMergedMod;
				}
			});

			this.WhenAnyValue(x => x.IsForceLoaded, x => x.IsEditorMod).Subscribe((b) =>
			{
				var isForceLoaded = b.Item1;
				var isEditorMod = b.Item2;

				if (isForceLoaded)
				{
					this.SelectedColor = "#64F38F00";
					this.ListColor = "#32C17200";
					HasColorOverride = true;
				}
				else if (isEditorMod)
				{
					this.SelectedColor = "#6400ED48";
					this.ListColor = "#0C00FF4D";
					HasColorOverride = true;
				}
				else
				{
					HasColorOverride = false;
				}
			});

			if (isBaseGameMod)
			{
				this.IsHidden = true;
				this.IsLarianMod = true;
			}

			// If a screen reader is active, don't bother making tooltips for the mod item entry
			hasToolTip = this.WhenAnyValue(x => x.Description, x => x.HasDependencies, x => x.UUID).
				Select(x => !DivinityApp.IsScreenReaderActive() && (
				!String.IsNullOrEmpty(x.Item1) || x.Item2 || !String.IsNullOrEmpty(x.Item3))).StartWith(true).ToProperty(this, nameof(HasToolTip));

			_canDelete = this.WhenAnyValue(x => x.IsEditorMod, x => x.IsLarianMod, x => x.FilePath,
				(isEditorMod, isLarianMod, path) => !isEditorMod && !isLarianMod && File.Exists(path)).StartWith(false).ToProperty(this, nameof(CanDelete));
			_canAddToLoadOrder = this.WhenAnyValue(x => x.ModType, x => x.IsLarianMod, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod, x => x.ForceAllowInLoadOrder,
				(modType, isLarianMod, isForceLoaded, isMergedMod, forceAllowInLoadOrder) => modType != "Adventure" && !isLarianMod && (!isForceLoaded || isMergedMod) || forceAllowInLoadOrder).StartWith(true).ToProperty(this, nameof(CanAddToLoadOrder));
		}
	}
}
