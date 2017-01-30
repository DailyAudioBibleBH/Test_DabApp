using System;
using System.ComponentModel;

namespace DABApp
{
	public class AudioPlayer : INotifyPropertyChanged
	{
		// Singleton for use throughout the app
		public static AudioPlayer Current = new AudioPlayer();

		//Don't allow creation of the class elsewhere in the app.
		private AudioPlayer()
		{
		}

		private bool _IsLoaded = true;
		private bool _IsPlaying = false;

		//property for whether or not a file is loaded (and whether to display the player bar)
		public bool IsLoaded
		{
			get
			{
				return _IsLoaded;
			}
			set
			{
				_IsLoaded = value;
				OnPropertyChanged("IsLoaded");
			}
		}

		//property for whether a file is being played or not
		public bool IsPlaying
		{
			get
			{
				return _IsPlaying;
			}
			set
			{
				_IsPlaying = value;
				OnPropertyChanged("IsPlaying");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged == null)
				return;

			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
