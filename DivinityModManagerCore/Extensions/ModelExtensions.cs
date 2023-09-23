using ReactiveUI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Extensions
{
	public static class ModelExtensions
	{
		public static void SetToDefault(this ReactiveObject model)
		{
			var props = TypeDescriptor.GetProperties(model.GetType());
			foreach (PropertyDescriptor pr in props)
			{
				if (pr.CanResetValue(model))
				{
					pr.ResetValue(model);
				}
			}
		}

		public static void SetFrom<T>(this T target, T from) where T : ReactiveObject
		{
			var props = TypeDescriptor.GetProperties(target.GetType());
			foreach (PropertyDescriptor pr in props)
			{
				var value = pr.GetValue(from);
				if (value != null)
				{
					pr.SetValue(target, value);
				}
			}
		}
	}
}
