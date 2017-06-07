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
		private double _CurrentTime = 0;
		private double _TotalTime = 1;
		private string _RemainingTime = "01:00";
		private bool _ShowPlayerBar = false;
		private string _CurrentEpisodeTitle;
		private string _CurrentTimeString = "00:00";

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

			// Start a timer to get time information from the player
			Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
						{
							if (_player.IsInitialized)
							{
								//Update current time
								if (_CurrentTime != _player.CurrentTime)
								{
									if (_CurrentTime < 0) {
										_CurrentTime = 0;
									}
									CurrentTime = _player.CurrentTime;
									if (!Double.IsNaN(_player.TotalTime)) {
										var t = TimeSpan.FromSeconds(TotalTime);
										var c = TimeSpan.FromSeconds(CurrentTime);
										var r = new TimeSpan(t.Days, t.Hours, t.Minutes, t.Seconds, 0) - new TimeSpan(c.Days, c.Hours, c.Minutes, c.Seconds, 0);
						            if(r.Hours == 0)
									{
										RemainingTime = $"{r.Minutes:D2}:{r.Seconds:D2}";
									}
									else { 
									 RemainingTime = $"{r.Hours:D2}:{r.Minutes:D2}:{r.Seconds:D2}";
									}
									}
								}

								if (_TotalTime != _player.TotalTime && !Double.IsNaN(_player.TotalTime))
								{
									TotalTime = _player.TotalTime;
									var t = TimeSpan.FromSeconds(TotalTime);
									var c = TimeSpan.FromSeconds(CurrentTime);
									var r = new TimeSpan(t.Days, t.Hours, t.Minutes, t.Seconds, 0) - new TimeSpan(c.Days, c.Hours, c.Minutes, c.Seconds, 0);
						            if(r.Hours == 0)
									{
										RemainingTime = $"{r.Minutes:D2}:{r.Seconds:D2}";
									}
									else { 
									 RemainingTime = $"{r.Hours:D2}:{r.Minutes:D2}:{r.Seconds:D2}";
									}
								}
								if (_IsPlaying != Player.IsPlaying)
								{
									_IsPlaying = Player.IsPlaying;
									OnPropertyChanged("PlayPauseButtonImageBig");
									//TODO: Do we need to change out the PlayPauseButtonImage here?
								}

								if (_IsInitialized != Player.IsInitialized)
								{
									IsInitialized = Player.IsInitialized;
								}

								if (_TotalTime == _CurrentTime) {
									PlayerFeedAPI.UpdateEpisodeProperty(Instance.CurrentEpisodeId);
									AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "stop", TotalTime);
									PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, 0, "0:00");
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
		private IAudio Player
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
		//Set Audio File
		public void SetAudioFile(string FileName)
		{
			_player.SetAudioFile(FileName, new dbEpisodes());
		}

		public void SetAudioFile(dbEpisodes episode)
		{
			if (_player.IsPlaying) {
				PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, CurrentTime, RemainingTime);
			}
			Instance.CurrentEpisodeId = episode.id;
			Instance.CurrentEpisodeTitle = episode.title;
			if (episode.is_downloaded)
			{
				_player.SetAudioFile($"{episode.id}.mp4", episode);
			}
			else
			{
				_player.SetAudioFile(episode.url, episode);
			}
			CurrentTime = episode.stop_time;
			RemainingTime = episode.remaining_time;
		}


		public string PlayPauseButtonImage
		{
			get
			{
				if (IsPlaying)
				{
					//Pause
					return "ic_pause_circle_outline_white.png";
				}
				else {
					//Play
					return "ic_play_circle_outline_white.png";
				}
			} 
			set
			{
				throw new Exception("You can't set this directly.");
			}
		}

		public string PlayPauseButtonImageBig
		{
			get
			{
				if (IsPlaying)
				{
					//Pause
					return "ic_pause_circle_outline_white_2x.png";
				}
				else
				{
					//Play
					return "ic_play_circle_outline_white_2x.png";
				}
			}
			set
			{
				throw new Exception("You can't set this directly.");
			}
		}

		//Play
		public void Play()
		{
			if (!IsPlaying)
			{
				_player.Play();
				OnPropertyChanged("PlayPauseButtonImage");
				OnPropertyChanged("PlayPauseButtonImageBig");
				AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "play", CurrentTime);
			}
		}

		//Pause
		public void Pause()
		{
			if (IsPlaying)
			{
				_player.Pause();
				OnPropertyChanged("PlayPauseButtonImage");
				OnPropertyChanged("PlayPauseButtonImageBig");
				AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "pause", CurrentTime);
				PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, CurrentTime, RemainingTime);
			}
		}

		public void Unload(){
			_player.Unload();
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
					else if (Convert.ToInt32(value) >= Convert.ToInt32(_player.CurrentTime + 5) || Convert.ToInt32(value) <= Convert.ToInt32(_player.CurrentTime - 5))
					{
						Player.SeekTo(Convert.ToInt32(value));
					}
				}
				_CurrentTime = value;
				OnPropertyChanged("CurrentTime");
				OnPropertyChanged("RemainingTime");
				OnPropertyChanged("Progress");
			}
		}

		public string CurrentTimeString { 
			get {
				return _CurrentTimeString;
			}
			set {
				_CurrentTimeString = value;
				OnPropertyChanged("CurrentTimeString");
			}
		}

		public string RemainingTime
		{
			get
			{
				return _RemainingTime;
			}
			set {
				_RemainingTime = value;
				OnPropertyChanged("RemainingTime");
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
			get
			{
				return _CurrentTime / _TotalTime;
			}
		}

		public int CurrentEpisodeId { get; set; } = 0;
		public string CurrentEpisodeTitle
		{
			get { return _CurrentEpisodeTitle; }
			set
			{
				_CurrentEpisodeTitle = value;
				OnPropertyChanged("CurrentEpisodeTitle");
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
			//CurrentTime = seconds;
		}

		public void GetOutputs() {
			_player.SwitchOutputs();
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

		private static void NotifyStaticPropertyChanged(string propertyName)
		{
			StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
	}
}
