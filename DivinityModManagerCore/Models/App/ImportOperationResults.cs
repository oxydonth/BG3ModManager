using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.App
{
	public class ImportOperationResults
	{
		public bool Success { get; set; }
		public List<DivinityModData> Mods { get; set; } = new List<DivinityModData>();
	}
}
