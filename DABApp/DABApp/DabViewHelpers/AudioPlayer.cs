using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace DABApp
{
	public class AudioPlayer : INotifyPropertyChanged
	{
		private IAudio _player;
		private bool _IsInitialized = false;
		private bool _IsPlaying = false;
		private string _PlayButtonText = "ic_play_circle_outline_white_3x.png";
		private double _CurrentTime = 0;
		private double _TotalTime = 1;
		private bool _ShowPlayerBar = false;

		// Singleton for use throughout the app
		public static AudioPlayer Instance { get; private set; }
		static AudioPlayer()
		{
			Instance = new AudioPlayer();
		}

		//Don't allow creation of the class elsewhere in the app.
		private AudioPlayer()
		{
			//Create a player object 
			_player = DependencyService.Get<IAudio>();

			Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
						{
							if (_player.IsInitialized)
							{
								//Update current time
								if (_CurrentTime != _player.CurrentTime && _CurrentTime >=0)
								{
									CurrentTime = _player.CurrentTime;
								}

								if (_TotalTime != _player.TotalTime && !Double.IsNaN(_player.TotalTime))
								{
									TotalTime = _player.TotalTime;
								}

								if (_IsPlaying != Player.IsPlaying)
								{
									_IsPlaying = Player.IsPlaying;
								}

								if (_IsInitialized != Player.IsInitialized)
								{
									IsInitialized = Player.IsInitialized;
								}
								if (showPlayerBar)
								{
									_ShowPlayerBar = IsInitialized;
								}
								else {
									_ShowPlayerBar = showPlayerBar;
					}
								
							}
							else {
								_CurrentTime = 0;
								_TotalTime = 1;
								_IsInitialized = false;
								_IsPlaying = false;
							}
							return true;
						}
						);
		}


		//Reference to the player
		public IAudio Player
		{
			get
			{
				return _player;
			}
		}


	//property for whether or not a file is loaded (and whether to display the player bar)
	public bool IsInitialized
	{
		get
		{
			return Player.IsInitialized;
		}
		set
		{
			_IsInitialized = value;
			OnPropertyChanged("IsInitialized");
		}
	}


	//property for whether a file is being played or not
	public bool IsPlaying
	{
		get
		{
			return _player.IsPlaying;
		}
			set
			{
				_IsPlaying = value;
				OnPropertyChanged("IsPlaying");
			}
	}

	//Title of the audio file
	public string PlayButtonText
	{
		get
		{
			return _PlayButtonText;
		}
		set
		{
			_PlayButtonText = value;
			OnPropertyChanged("PlayButtonText");
		}
	}

	public double CurrentTime
	{
		get
		{
			return _CurrentTime;
		}
		set
		{
				if (Convert.ToInt32(value) != Convert.ToInt32(_player.CurrentTime) && value != 1)
				{
					if (Device.OS == TargetPlatform.iOS)
					{
						Player.SeekTo(Convert.ToInt32(value));
					}
					else if (Convert.ToInt32(value) >= Convert.ToInt32(_player.CurrentTime + 3) || Convert.ToInt32(value) <= Convert.ToInt32(_player.CurrentTime - 3)) {
						Player.SeekTo(Convert.ToInt32(value));
					}
				}
			_CurrentTime = value;
			OnPropertyChanged("CurrentTime");
			OnPropertyChanged("RemainingTime");
				OnPropertyChanged("Progress");
		}
	}

	public double RemainingTime
	{
		get
		{
			return _TotalTime - _CurrentTime;
		}
	}

	public double TotalTime
	{
		get
		{
			return _TotalTime;
		}
		set
		{
			_TotalTime = value;
			OnPropertyChanged("TotalTime");
		}
	}


		public double Progress 
		{ 
			get {
				return _CurrentTime / _TotalTime;
			}
		}

		public bool ShowPlayerBar { 
			get {
					return _ShowPlayerBar;
			}
			set {
				_ShowPlayerBar = value;
				OnPropertyChanged("ShowPlayerBar");
			}
		}
		public bool showPlayerBar { get; set; } = true;

	public void SeekTo(int seconds)
	{
		_player.SeekTo(seconds);
		//Update the current time
		CurrentTime = seconds;
	}

		public void Skip(int seconds)
		{

			_player.Skip(seconds);
			//Update the current time
			CurrentTime = seconds;
		}



	public void SetAudioFile(string FileName)
	{
		_player.SetAudioFile(FileName);
	}

	protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
	{
		var handler = PropertyChanged;
		if (handler != null)
			handler(this, new PropertyChangedEventArgs(propertyName));
	}

	public event PropertyChangedEventHandler PropertyChanged;
}
}
