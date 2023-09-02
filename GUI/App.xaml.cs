using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Util;

using ReactiveUI;

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace DivinityModManager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			Directory.SetCurrentDirectory(DivinityApp.GetAppDirectory());
			// Fix for loading C++ dlls from _Lib
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			// POCO type warning suppression
			Splat.Locator.CurrentMutable.Register(() => new DivinityModManager.Util.CustomPropertyResolver(), typeof(ICreatesObservableForProperty));
			WebHelper.SetupClient();
#if DEBUG
			RxApp.SuppressViewCommandBindingMessage = false;
#else
			RxApp.DefaultExceptionHandler = new RxExceptionHandler();
			RxApp.SuppressViewCommandBindingMessage = true;
#endif
		}

		private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var assyName = new AssemblyName(args.Name);

			var newPath = Path.Combine("_Lib", assyName.Name);
			if (!newPath.EndsWith(".dll"))
			{
				newPath += ".dll";
			}

			if (File.Exists(newPath))
			{
				var assy = Assembly.LoadFile(newPath);
				return assy;
			}
			return null;
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			//For making date display use the current system's culture
			FrameworkElement.LanguageProperty.OverrideMetadata(
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

			EventManager.RegisterClassHandler(typeof(Window), Window.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown));
		}

		private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			DivinityApp.IsKeyboardNavigating = false;
		}
	}
}
