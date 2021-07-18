using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModVersion : ReactiveObject
	{
		private long major = 0;

		public long Major
		{
			get { return major; }
			set
			{
				this.RaiseAndSetIfChanged(ref major, value);
				UpdateVersion();
			}
		}

		private long minor = 0;

		public long Minor
		{
			get { return minor; }
			set
			{
				this.RaiseAndSetIfChanged(ref minor, value);
				UpdateVersion();
			}
		}

		private long revision = 0;

		public long Revision
		{
			get { return revision; }
			set
			{
				this.RaiseAndSetIfChanged(ref revision, value);
				UpdateVersion();
			}
		}

		private long build = 0;

		public long Build
		{
			get { return build; }
			set
			{
				this.RaiseAndSetIfChanged(ref build, value);
				UpdateVersion();
			}
		}

		private string version;

		[JsonProperty]
		public string Version
		{
			get { return version; }
			set
			{
				this.RaiseAndSetIfChanged(ref version, value);
			}
		}

		private long versionInt = 0;

		[JsonProperty]
		public long VersionInt
		{
			get { return versionInt; }
			set
			{
				value = Math.Max(long.MinValue, Math.Min(value, long.MaxValue));
				if(versionInt != value)
				{
					ParseInt(versionInt);
					this.RaisePropertyChanged("VersionInt");
				}
			}
		}

		private void UpdateVersion()
		{
			Version = String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
			var nextVersion = ToInt();
			if(nextVersion != versionInt)
			{
				versionInt = ToInt();
				this.RaisePropertyChanged("VersionInt");
			}
		}

		public long ToInt()
		{
			return (Major << 55) + (Minor << 47) + (Revision << 31) + (Build);
		}

		public override string ToString()
		{
			return String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
		}

		public void ParseInt(long vInt, bool update=true)
		{
			if(versionInt != vInt)
			{
				versionInt = vInt;
				this.RaisePropertyChanged("VersionInt");
			}
			major = (sbyte)(versionInt >> 55);
			minor = (sbyte)(versionInt >> 47);
			revision = (Int16)(versionInt >> 31) & 0xFF;
			build = (versionInt & 0x7FFFFFFF);
			if (update)
			{
				UpdateVersion();
			}
			this.RaisePropertyChanged("Major");
			this.RaisePropertyChanged("Minor");
			this.RaisePropertyChanged("Revision");
			this.RaisePropertyChanged("Build");
		}

		public static DivinityModVersion FromInt(int vInt)
		{
			return new DivinityModVersion(vInt);
		}

		public DivinityModVersion() { }

		public DivinityModVersion(int vInt)
		{
			ParseInt(vInt);
		}

		public DivinityModVersion(int headerMajor, int headerMinor, int headerRevision, int headerBuild)
		{
			Major = headerMajor;
			Minor = headerMinor;
			Revision = headerRevision;
			Build = headerBuild;
		}
	}
}
