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

namespace DivinityModManager.Views
{
	public class SettingsWindowBase : HideWindowBase<DivinityModManagerSettings> { }

	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : SettingsWindowBase
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		public void Init(MainWindowViewModel vm)
		{
			ViewModel = vm.Settings;
			DataContext = ViewModel;

			BindingHelper.CreateCommandBinding(this.SaveSettingsButton, "SaveSettingsCommand", ViewModel);

			KeybindingsListView.ItemsSource = vm.Keys.All;
			KeybindingsListView.SetBinding(ListView.ItemsSourceProperty, new Binding("All")
			{
				Source = vm.Keys,
				Mode = BindingMode.OneWay
			});
			KeybindingsListView.SetBinding(ListView.SelectedItemProperty, new Binding("SelectedHotkey")
			{
				Source = ViewModel,
				Mode = BindingMode.OneWayToSource
			});

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
		}

		private bool isSettingKeybinding = false;

		private void FocusSelectedHotkey()
		{
			ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
			var hotkeyControls = row.FindVisualChildren<HotkeyEditorControl>();
			foreach (var c in hotkeyControls)
			{
				c.HotkeyTextBox.Focus();
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
				if (ViewModel.ExtenderTabIsVisible)
				{
					ViewModel.ExportExtenderSettingsCommand.Execute(null);
				}
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
			lastWorkshopPath = ViewModel.WorkshopPath;
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
