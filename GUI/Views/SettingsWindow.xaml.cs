using DivinityModManager.Models;
using DivinityModManager.Util;
using DivinityModManager.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using System.Diagnostics;
using System.Globalization;
using DivinityModManager.Controls;
using Xceed.Wpf.Toolkit;
using DivinityModManager.Converters;
using DivinityModManager.Models.Extender;
using System.Reflection;
using WpfAutoGrid;
using DivinityModManager.Controls.Extensions;
using System.Collections.ObjectModel;
using DivinityModManager.Models.View;

namespace DivinityModManager.Views
{
	public class SettingsWindowBase : HideWindowBase<SettingsWindowViewModel> { }

	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : SettingsWindowBase
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		private void CreateSettingsElements(object source, Type settingsModelType, AutoGrid targetGrid, int startRow = 0)
		{
			var props = settingsModelType.GetProperties()
				.Select(x => SettingsAttributeProperty.FromProperty(x))
				.Where(x => x.Attribute != null).ToList();

			int count = props.Count + 1;
			int row = startRow;

			targetGrid.Children.Clear();

			targetGrid.RowCount = count;
			targetGrid.Rows = String.Join(",", Enumerable.Repeat("auto", count));

			var debugModeBinding = new Binding(nameof(SettingsWindowViewModel.DeveloperModeVisibility))
			{
				Source = ViewModel,
				FallbackValue = Visibility.Collapsed
			};

			foreach (var prop in props)
			{
				var targetRow = row;
				row++;
				var tb = new TextBlock
				{
					Text = prop.Attribute.DisplayName,
					ToolTip = prop.Attribute.Tooltip
				};
				targetGrid.Children.Add(tb);
				Grid.SetRow(tb, targetRow);

				if (prop.Attribute.IsDebug)
				{
					tb.SetBinding(TextBlock.VisibilityProperty, debugModeBinding);
				}

				if(settingsModelType.Name == "ScriptExtenderUpdateConfig" && prop.Property.Name == "TargetVersion")
				{
					if(TryFindResource("UpdaterTargetVersionComboBox") is ComboBox cb)
					{
						targetGrid.Children.Add(cb);
						Grid.SetRow(cb, targetRow);
						Grid.SetColumn(cb, 1);
					}
					continue;
				}

				if(prop.Property.PropertyType.IsEnum)
				{
					var combo = new ComboBox()
					{
						ToolTip = prop.Attribute.Tooltip,
						DisplayMemberPath = "Description",
						SelectedValuePath = "Value"
					};
					var items = new ObservableCollection<EnumEntry>();
					var values = prop.Property.PropertyType.GetEnumValues().Cast<Enum>().Select(x => new EnumEntry(x.GetDescription(), x)).OrderBy(x => x.Description);
					items.AddRange(values);
					combo.SetBinding(ComboBox.ItemsSourceProperty, new Binding()
					{
						Source = items,
						Mode = BindingMode.OneWay
					});
					combo.SetBinding(ComboBox.SelectedValueProperty, new Binding(prop.Property.Name)
					{
						Source = source,
						Mode = BindingMode.OneWay
					});
					targetGrid.Children.Add(combo);
					Grid.SetRow(combo, targetRow);
					Grid.SetColumn(combo, 1);
					continue;
				}

				var propType = Type.GetTypeCode(prop.Property.PropertyType);

				switch (propType)
				{
					case TypeCode.Boolean:
						CheckBox cb = new CheckBox
						{
							ToolTip = prop.Attribute.Tooltip,
							VerticalAlignment = VerticalAlignment.Center
						};
						//cb.HorizontalAlignment = HorizontalAlignment.Right;
						cb.SetBinding(CheckBox.IsCheckedProperty, new Binding(prop.Property.Name)
						{
							Source = source,
							Mode = BindingMode.TwoWay,
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						if (prop.Attribute.IsDebug)
						{
							cb.SetBinding(CheckBox.VisibilityProperty, debugModeBinding);
						}
						targetGrid.Children.Add(cb);
						Grid.SetRow(cb, targetRow);
						Grid.SetColumn(cb, 1);
						break;

					case TypeCode.String:
						UnfocusableTextBox utb = new UnfocusableTextBox
						{
							ToolTip = prop.Attribute.Tooltip,
							VerticalAlignment = VerticalAlignment.Center,
							//utb.HorizontalAlignment = HorizontalAlignment.Stretch;
							TextAlignment = TextAlignment.Left
						};
						utb.SetBinding(UnfocusableTextBox.TextProperty, new Binding(prop.Property.Name)
						{
							Source = source,
							Mode = BindingMode.TwoWay,
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						if (prop.Attribute.IsDebug)
						{
							utb.SetBinding(CheckBox.VisibilityProperty, debugModeBinding);
						}
						targetGrid.Children.Add(utb);
						Grid.SetRow(utb, targetRow);
						Grid.SetColumn(utb, 1);
						break;
					case TypeCode.Int32:
					case TypeCode.Int64:
						var ud = new IntegerUpDown();
						ud.ToolTip = prop.Attribute.Tooltip;
						ud.VerticalAlignment = VerticalAlignment.Center;
						ud.HorizontalAlignment = HorizontalAlignment.Left;
						ud.Padding = new Thickness(4, 2, 4, 2);
						ud.AllowTextInput = true;
						ud.SetBinding(IntegerUpDown.ValueProperty, new Binding(prop.Property.Name)
						{
							Source = ViewModel.ExtenderSettings,
							Mode = BindingMode.TwoWay,
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						if (prop.Attribute.IsDebug)
						{
							ud.SetBinding(VisibilityProperty, debugModeBinding);
						}
						targetGrid.Children.Add(ud);
						Grid.SetRow(ud, targetRow);
						Grid.SetColumn(ud, 1);
						break;
				}
			}
		}

		private SettingsWindowTab IndexToTab(int index)
		{
			return (SettingsWindowTab)index;
		}

		private int TabToIndex(SettingsWindowTab tab)
		{
			return (int)tab;
		}

		public void Init(MainWindowViewModel main)
		{
			ViewModel = new SettingsWindowViewModel(this, main);
			main.WhenAnyValue(x => x.Settings).BindTo(ViewModel, vm => vm.Settings);

			this.KeyDown += SettingsWindow_KeyDown;
			KeybindingsListView.Loaded += (o, e) =>
			{
				if (KeybindingsListView.SelectedIndex < 0)
				{
					KeybindingsListView.SelectedIndex = 0;
				}
				ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
				if (row != null && !FocusHelper.HasKeyboardFocus(row))
				{
					Keyboard.Focus(row);
				}
			};
			KeybindingsListView.KeyUp += KeybindingsListView_KeyUp;

			CreateSettingsElements(ViewModel.Settings, typeof(DivinityModManagerSettings), SettingsAutoGrid);
			CreateSettingsElements(ViewModel.ExtenderSettings, typeof(ScriptExtenderSettings), ExtenderSettingsAutoGrid);
			CreateSettingsElements(ViewModel.ExtenderUpdaterSettings, typeof(ScriptExtenderUpdateConfig), ExtenderUpdaterSettingsAutoGrid);

			this.OneWayBind(ViewModel, vm => vm.Main.Keys.All, view => view.KeybindingsListView.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedHotkey, view => view.KeybindingsListView.SelectedItem);

			this.Bind(ViewModel, vm => vm.Settings.DebugModeEnabled, view => view.DebugModeCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.Settings.LogEnabled, view => view.LogEnabledCheckBox.IsChecked);
			this.OneWayBind(ViewModel, vm => vm.LaunchParams, view => view.GameLaunchParamsMainMenuItem.ItemsSource);
			this.Bind(ViewModel, vm => vm.Settings.GameLaunchParams, view => view.GameLaunchParamsTextBox.Text);

			this.Bind(ViewModel, vm => vm.SelectedTabIndex, view => view.PreferencesTabControl.SelectedIndex, tab => TabToIndex(tab), index => IndexToTab(index));
			this.OneWayBind(ViewModel, vm => vm.ExtenderTabVisibility, view => view.ScriptExtenderTab.Visibility);
			this.OneWayBind(ViewModel, vm => vm.ExtenderUpdaterVisibility, view => view.ScriptExtenderUpdaterTab.Visibility);
			this.OneWayBind(ViewModel, vm => vm.ResetSettingsCommandToolTip, view => view.ResetSettingsButton.ToolTip);

			this.BindCommand(ViewModel, vm => vm.SaveSettingsCommand, view => view.SaveSettingsButton);
			this.BindCommand(ViewModel, vm => vm.OpenSettingsFolderCommand, view => view.OpenSettingsFolderButton);
			this.BindCommand(ViewModel, vm => vm.ResetSettingsCommand, view => view.ResetSettingsButton);
			this.BindCommand(ViewModel, vm => vm.ClearLaunchParamsCommand, view => view.ClearLaunchParamsMenuItem);
			this.BindCommand(ViewModel, vm => vm.ClearCacheCommand, view => view.ClearCacheButton);

			this.Events().IsVisibleChanged.InvokeCommand(ViewModel.OnWindowShownCommand);

			DataContext = ViewModel;
		}

		private bool isSettingKeybinding = false;

		private void ClearFocus()
		{
			foreach (var item in KeybindingsListView.Items)
			{
				if (item is HotkeyEditorControl hotkey && hotkey.IsEditing)
				{
					hotkey.SetEditing(false);
				}
			}
		}

		private void FocusSelectedHotkey()
		{
			ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
			var hotkeyControls = row.FindVisualChildren<HotkeyEditorControl>();
			foreach (var c in hotkeyControls)
			{
				c.SetEditing(true);
				isSettingKeybinding = true;
			}
		}

		private void KeybindingsListView_KeyUp(object sender, KeyEventArgs e)
		{
			if (KeybindingsListView.SelectedIndex >= 0 && e.Key == Key.Enter)
			{
				FocusSelectedHotkey();
			}
		}

		private void SettingsWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if (isSettingKeybinding)
			{
				return;
			}
			else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				ViewModel.SaveSettingsCommand.Execute(null);
				e.Handled = true;
			}
			else if (e.Key == Key.Left && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				int current = PreferencesTabControl.SelectedIndex;
				int nextIndex = current - 1;
				if (nextIndex < 0)
				{
					nextIndex = PreferencesTabControl.Items.Count - 1;
				}
				PreferencesTabControl.SelectedIndex = nextIndex;
				Keyboard.Focus((FrameworkElement)PreferencesTabControl.SelectedContent);
				MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			}
			else if (e.Key == Key.Right && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				int current = PreferencesTabControl.SelectedIndex;
				int nextIndex = current + 1;
				if (nextIndex >= PreferencesTabControl.Items.Count)
				{
					nextIndex = 0;
				}
				PreferencesTabControl.SelectedIndex = nextIndex;
				//Keyboard.Focus((FrameworkElement)PreferencesTabControl.SelectedContent);
				//MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			}
		}

		private string lastWorkshopPath = "";

		public EventHandler OnWorkshopPathChanged { get; set; }

		private void WorkshopPathTextbox_GotFocus(object sender, RoutedEventArgs e)
		{
			lastWorkshopPath = ViewModel.Settings.WorkshopPath;
		}

		private void WorkshopPathTextbox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox tb && tb.Text != lastWorkshopPath)
			{
				OnWorkshopPathChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		private void HotkeyEditorControl_GotFocus(object sender, RoutedEventArgs e)
		{
			isSettingKeybinding = true;
		}

		private void HotkeyEditorControl_LostFocus(object sender, RoutedEventArgs e)
		{
			isSettingKeybinding = false;
		}

		private void HotkeyListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			FocusSelectedHotkey();
		}
	}
}
