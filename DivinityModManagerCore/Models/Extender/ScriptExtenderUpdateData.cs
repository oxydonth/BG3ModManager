using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Extender
{
	public class ScriptExtenderUpdateData
	{
		public int ManifestMinorVersion { get; set; }
		public int ManifestVersion { get; set; }
		public string NoMatchingVersionNotice { get; set; }
		public List<ScriptExtenderUpdateResource> Resources { get; set; }
	}

	public class ScriptExtenderUpdateResource
	{
		public string Name { get; set; }
		public List<ScriptExtenderUpdateVersion> Versions { get; set; }
	}

	public class ScriptExtenderUpdateVersion : ReactiveObject
	{
		[Reactive] public object BuildDate { get; set; }
		[Reactive] public string Digest { get; set; }
		[Reactive] public string MinGameVersion { get; set; }
		[Reactive] public string Notice { get; set; }
		[Reactive] public string URL { get; set; }
		[Reactive] public string Version { get; set; }
		[Reactive] public string Signature { get; set; }

		private ObservableAsPropertyHelper<string> _tooltip;

		public string ToolTip => _tooltip.Value;

		private string PropertiesToToolTip(ValueTuple<string, string, string> values)
		{
			return $"Version:{values.Item1}\nGame Version: {values.Item2}\nURL:";
		}

		public ScriptExtenderUpdateVersion()
		{
			_tooltip = this.WhenAnyValue(x => x.Version, x => x.MinGameVersion, x => x.URL).Select(PropertiesToToolTip).ToProperty(this, nameof(ToolTip), scheduler: RxApp.MainThreadScheduler);
		}
	}
}
