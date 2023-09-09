using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using NexusModsNET.DataModels;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.NexusMods
{
	public class NexusModsModData : NexusMod, INotifyPropertyChanged
	{
		[JsonProperty("uuid")]
		public string UUID { get; set; }

		private long _lastFileId;

		[JsonProperty("last_file_id")]
		public long LastFileId
		{
			get => _lastFileId;
			set
			{
				if(_lastFileId != value)
				{
					_lastFileId = value;
					RaisePropertyChanged(nameof(LastFileId));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void SetModVersion(NexusModFileVersionData info)
		{
			if(info.Success)
			{
				SetModVersion(info.ModId, info.FileId);
			}
		}

		public void SetModVersion(long modId, long fileId = -1)
		{
			if (ModId != modId)
			{
				ModId = modId;
				RaisePropertyChanged(nameof(ModId));
			}

			if (fileId > -1 && LastFileId != fileId)
			{
				LastFileId = fileId;
			}
		}

		public void Update(NexusMod data)
		{
			var properties = typeof(NexusMod)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(prop => prop.GetCustomAttribute<JsonPropertyAttribute>(true) != null);
			foreach(var prop in properties)
			{
				var value = prop.GetValue(data);
				if(value != null)
				{
					prop.SetValue(this, value);
					RaisePropertyChanged(prop.Name);
				}
			}
			IsUpdated = true;
		}

		[JsonIgnore]
		public bool IsUpdated { get; set; }

		public NexusModsModData()
		{
			ModId = -1;
			LastFileId = -1;
		}
	}
}
