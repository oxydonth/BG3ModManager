using System;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;

namespace DivinityModManager
{
	public class SettingsEntryAttribute : Attribute
	{
		public string DisplayName { get; set; }
		public string Tooltip { get; set; }
		public bool IsDebug { get; set; }
		public SettingsEntryAttribute(string displayName = "", string tooltip = "", bool isDebug = false)
		{
			DisplayName = displayName;
			Tooltip = tooltip;
			IsDebug = isDebug;
		}
	}

	public struct SettingsAttributeProperty
	{
		public PropertyInfo Property { get; set; }
		public SettingsEntryAttribute Attribute { get; set; }

		public static SettingsAttributeProperty FromProperty(PropertyInfo property)
		{
			return new SettingsAttributeProperty
			{
				Property = property,
				Attribute = property.GetCustomAttribute<SettingsEntryAttribute>()
			};
		}
	}

}
