using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using NexusModsNET.DataModels;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.NexusMods
{
	public class NexusModsModData : NexusMod
	{
		[JsonProperty("uuid")]
		public string UUID { get; set; }

		[JsonProperty("last_file_id")]
		public long LastFileId { get; set; }

		public void SetModVersion(NexusModFileVersionData info)
		{
			if(info.Success)
			{
				ModId = info.ModId;
				LastFileId = info.FileId;
			}
		}

		public void SetModVersion(long modId, long fileId = -1)
		{
			ModId = modId;
			if(fileId > -1)
			{
				LastFileId = fileId;
			}
		}

		public void Update(NexusMod data)
		{
			var properties = typeof(NexusMod)
			.GetRuntimeProperties()
			.Where(prop => Attribute.IsDefined(prop, typeof(JsonProperty)));
			foreach(var prop in properties)
			{
				prop.SetValue(this, prop.GetValue(data));
			}
			IsUpdated = true;
		}

		[JsonIgnore]
		public bool IsUpdated { get; set; }
	}
}
