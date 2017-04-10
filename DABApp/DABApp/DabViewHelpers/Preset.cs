using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DABApp
{
	public class Preset : INotifyPropertyChanged
	{
		private bool _Selected = false;
		public string duration { get; set; }
		public bool Selected { get { return _Selected;} 
			set {
				_Selected = value;
				OnPropertyChanged("Selected");
			} }
		public Preset(string Duration, bool selected)
		{
			duration = Duration;
			Selected = selected;
		}
		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			handler(this, new PropertyChangedEventArgs(propertyName));
		}
		public event PropertyChangedEventHandler PropertyChanged;
		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };
		private static void NotifyStaticPropertyChanged(string propertyName) { 
            StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class OfflineEpisodeSettings
	{
		public string Duration { get; set; }
		public bool DeleteAfterListening { get; set; }
		public static OfflineEpisodeSettings Instance { get; set; }
		static OfflineEpisodeSettings() {
			Instance = new OfflineEpisodeSettings();
		}
	}
}
