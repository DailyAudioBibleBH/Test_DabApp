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
		private string _PlayButtonText = "Play";
		private double _CurrentTime = 0;
		private double _TotalTime = 1;


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
								if (_CurrentTime != _player.CurrentTime)
								{
									CurrentTime = _player.CurrentTime;
								}

								if (_TotalTime != _player.TotalTime)
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
					Player.SeekTo(Convert.ToInt32(value));
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

		public List<string> CurrentOutputs { 
			get {
				return Player.CurrentOutputs;
			}
		}

		public double Progress 
		{ 
			get {
				return _CurrentTime / _TotalTime;
			}
		}

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
