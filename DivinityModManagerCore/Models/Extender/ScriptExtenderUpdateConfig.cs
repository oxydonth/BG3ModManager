using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Extender
{
	[DataContract]
	public class ScriptExtenderUpdateConfig : ReactiveObject
	{
		[SettingsEntry("UpdateChannel", "Use a specific update channel (Release or Devel)")]
		[Reactive]
		[DataMember]
		[DefaultValue("Release")]
		public string UpdateChannel { get; set; }

		[SettingsEntry("TargetVersion", "Update to a specific version of the script extender (ex. '4.0.0.0')")]
		[Reactive]
		[DataMember]
		[DefaultValue("")]
		public string TargetVersion { get; set; }

		[SettingsEntry("Disable Updates", "Disable automatic updating to the latest extender version")]
		[Reactive]
		[DataMember]
		[DefaultValue(false)]
		public bool DisableUpdates { get; set; }

		[SettingsEntry("Debug", "Enable debug mode, which prints more messages to the console window")]
		[Reactive]
		[DataMember]
		[DefaultValue(false)]
		public bool Debug { get; set; }

		[SettingsEntry("ManifestURL", "", true)]
		[Reactive]
		[DataMember]
		[DefaultValue("")]
		public string ManifestURL { get; set; }

		[SettingsEntry("ManifestName", "", true)]
		[Reactive]
		[DataMember]
		[DefaultValue("")]
		public string ManifestName { get; set; }

		[SettingsEntry("TargetResourceDigest", "", true)]
		[Reactive]
		[DataMember]
		[DefaultValue("")]
		public string TargetResourceDigest { get; set; }

		[SettingsEntry("CachePath", "", true)]
		[Reactive]
		[DataMember]
		[DefaultValue("")]
		public string CachePath { get; set; }

		[SettingsEntry("ValidateSignature", "", true)]
		[Reactive]
		[DataMember]
		[DefaultValue(false)]
		public bool ValidateSignature { get; set; }
	}
}
