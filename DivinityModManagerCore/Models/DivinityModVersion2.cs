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
	public class DivinityModVersion2 : ReactiveObject
	{
		private ulong major = 0;

		public ulong Major
		{
			get { return major; }
			set
			{
				this.RaiseAndSetIfChanged(ref major, value);
				UpdateVersion();
			}
		}

		private ulong minor = 0;

		public ulong Minor
		{
			get { return minor; }
			set
			{
				this.RaiseAndSetIfChanged(ref minor, value);
				UpdateVersion();
			}
		}

		private ulong revision = 0;

		public ulong Revision
		{
			get { return revision; }
			set
			{
				this.RaiseAndSetIfChanged(ref revision, value);
				UpdateVersion();
			}
		}

		private ulong build = 0;

		public ulong Build
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

		private ulong versionInt = 0;

		[JsonProperty]
		public ulong VersionInt
		{
			get { return versionInt; }
			set
			{
				value = Math.Max(ulong.MinValue, Math.Min(value, ulong.MaxValue));
				if (versionInt != value)
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
			if (nextVersion != versionInt)
			{
				versionInt = ToInt();
				this.RaisePropertyChanged("VersionInt");
			}
		}

		public ulong ToInt()
		{
			return (Major << 55) + (Minor << 47) + (Revision << 31) + Build;
		}

		public override string ToString()
		{
			return String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
		}

		public void ParseInt(ulong vInt, bool update = true)
		{
			if (versionInt != vInt)
			{
				versionInt = vInt;
				this.RaisePropertyChanged("VersionInt");
			}
			/*
			major = (sbyte)(versionInt >> 55);
			minor = (sbyte)(versionInt >> 47);
			revision = (Int16)(versionInt >> 31) & 0xFF;
			build = (versionInt & 0x7FFFFFFF);
			*/
			major = versionInt >> 55;
			minor = (versionInt >> 47) & 0xFF;
			revision = (versionInt >> 31) & 0xFFFF;
			build = versionInt & 0x7FFFFFFFUL;
			if (update)
			{
				UpdateVersion();
			}
			this.RaisePropertyChanged("Major");
			this.RaisePropertyChanged("Minor");
			this.RaisePropertyChanged("Revision");
			this.RaisePropertyChanged("Build");
		}

		public static DivinityModVersion2 FromInt(ulong vInt)
		{
			if (vInt == 1 || vInt == 268435456)
			{
				// 1.0.0.0
				vInt = 36028797018963968;
			}
			return new DivinityModVersion2(vInt);
		}

		public static bool operator >(DivinityModVersion2 a, DivinityModVersion2 b)
		{
			return a.VersionInt > b.VersionInt;
		}

		public static bool operator <(DivinityModVersion2 a, DivinityModVersion2 b)
		{
			return a.VersionInt < b.VersionInt;
		}

		public static bool operator >=(DivinityModVersion2 a, DivinityModVersion2 b)
		{
			return a.VersionInt >= b.VersionInt;
		}

		public static bool operator <=(DivinityModVersion2 a, DivinityModVersion2 b)
		{
			return a.VersionInt <= b.VersionInt;
		}

		public DivinityModVersion2() { }

		public DivinityModVersion2(ulong vInt)
		{
			ParseInt(vInt);
		}

		public DivinityModVersion2(ulong headerMajor, ulong headerMinor, ulong headerRevision, ulong headerBuild)
		{
			Major = headerMajor;
			Minor = headerMinor;
			Revision = headerRevision;
			Build = headerBuild;
		}

		public static readonly DivinityModVersion2 Empty = new DivinityModVersion2(0, 0, 0, 0);
	}
}
