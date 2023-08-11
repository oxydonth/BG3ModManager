using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyTitle("BG3ModManager")]
[assembly: AssemblyDescription("A mod manager for Baldur's Gate 3.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("LaughingLeader")]
[assembly: AssemblyProduct("Baldur's Gate 3 Mod Manager")]
[assembly: AssemblyCopyright("Copyright © 2020")]
[assembly: AssemblyTrademark("")]
[assembly: NeutralResourcesLanguageAttribute("en")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,
	ResourceDictionaryLocation.SourceAssembly
)]

[assembly: AssemblyVersion("1.0.9.1")]