using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace DivinityModManager.Models.Updates
{
	public enum UpdateSource
	{
		NONE,
		WORKSHOP,
		NEXUS,
		GITHUB
	}

	public class DivinityModUpdateData : ReactiveObject, ISelectable
	{
		[Reactive] public DivinityModData LocalMod { get; set; }
		[Reactive] public DivinityModData UpdatedMod { get; set; }
		[Reactive] public bool IsSelected { get; set; }
		[Reactive] public bool IsNewMod { get; set; }
		[Reactive] public bool CanDrag { get; set; }
		[Reactive] public Visibility Visibility { get; set; }
		[Reactive] public UpdateSource Source { get; set; }

		private readonly ObservableAsPropertyHelper<bool> _isEditorMod;
		public bool IsEditorMod => _isEditorMod.Value;

		private readonly ObservableAsPropertyHelper<string> _updateFilePath;
		public string UpdateFilePath => _updateFilePath.Value;

		public DivinityModUpdateData()
		{
			Source = UpdateSource.NONE;
			CanDrag = true;
			Visibility = Visibility.Visible;

			_isEditorMod = this.WhenAnyValue(x => x.UpdatedMod.IsEditorMod, x => x.LocalMod.IsEditorMod).Select(x => x.Item1 || x.Item2).ToProperty(this, nameof(IsEditorMod));
			_updateFilePath = this.WhenAnyValue(x => x.UpdatedMod.FilePath).ToProperty(this, nameof(UpdateFilePath));
		}
	}
}
