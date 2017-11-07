using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
	public class AudioPlayer : INotifyPropertyChanged
	{
		private IAudio _player;
		private bool _IsInitialized = false;
		private bool _IsPlaying = false;
		private bool _IsTouched = false;
		private double _CurrentTime = 0;
		private double _TotalTime = 1;
		private string _RemainingTime = "01:00";
		private string _CurrentEpisodeTitle;
		private string _CurrentChannelTitle;
		private string _CurrentTimeString = "00:00";
		private string _playerStatus = "";
		private bool ShowWarning = false;

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
			_player.Completed += OnCompleted;
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
									if (!Double.IsNaN(_player.TotalTime))
									{
										var t = TimeSpan.FromSeconds(TotalTime);
										var c = TimeSpan.FromSeconds(CurrentTime);
										var r = new TimeSpan(t.Days, t.Hours, t.Minutes, t.Seconds, 0) - new TimeSpan(c.Days, c.Hours, c.Minutes, c.Seconds, 0);
										if (r.TotalSeconds != 0)
										{
											if (r.Hours == 0)
											{
												RemainingTime = $"{r.Minutes:D2}:{r.Seconds:D2}";
											}
											else
											{
												RemainingTime = $"{r.Hours:D2}:{r.Minutes:D2}:{r.Seconds:D2}";
											}
										}
										else {
											CurrentTime = 0;
											RemainingTime = stringConvert(TotalTime);
											Pause();
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
									OnPropertyChanged("PlayPauseButtonImage");
									if (IsPlaying)
									{
										UpdatePlay();
									}
									else UpdatePause();
									//TODO: Do we need to change out the PlayPauseButtonImage here?
								}

								if (_IsInitialized != Player.IsInitialized)
								{
									IsInitialized = Player.IsInitialized;
								}

								//if (_TotalTime == _CurrentTime)
								//{
								//	PlayerFeedAPI.UpdateEpisodeProperty(Instance.CurrentEpisodeId);
								//	AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "stop", TotalTime);
								//	PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, 0, stringConvert(TotalTime));
								//	Unload();
								//	IsInitialized = false;
								//}

								if (!_player.PlayerCanKeepUp && ShowWarning && !_player.IsPlaying)
								{
									PlayerFailure.Invoke(this, new EventArgs());
									ShowWarning = false;
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
			Instance.CurrentChannelTitle = episode.channel_title;
			var ext = episode.url.Split('.').Last();
			if (episode.is_downloaded)
			{
				_player.SetAudioFile($"{episode.id.ToString()}.{ext}", episode);
			}
			else
			{
				_player.SetAudioFile(episode.url, episode);
			}
			if (episode.stop_time >= TotalTime)
			{
				CurrentTime = 0;
			}
			else
			{
				CurrentTime = episode.stop_time;
			}

			RemainingTime = episode.remaining_time;
			ShowWarning = false;
		}


		public string PlayPauseButtonImage
		{
			get
			{
				if (IsPlaying)
				{
					//Pause
					return "ic_pause_circle_outline_white_2x.png";
				}
				else {
					//Play
					return "ic_play_circle_outline_white_2x.png";
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
					return "ic_pause_circle_outline_white_3x.png";
				}
				else
				{
					//Play
					return "ic_play_circle_outline_white_3x.png";
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
				//UpdatePlay();
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
				//UpdatePause();
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

                if (value ==1)
                {
                    Debug.WriteLine("Setting currenttime to 1");

                }
                //Only seek to a new spot if the times differ by more than a few seconds

                double MinTimeToSkip = 3;
                double GoToTime = value;
                double PlayerTime = _player.CurrentTime;

                if (Math.Abs((GoToTime - PlayerTime)) > MinTimeToSkip)
                {
                    Player.SeekTo(Convert.ToInt32(GoToTime));
                } else {
                    Debug.WriteLine($"Ignoring current time change from {PlayerTime} to {GoToTime} because it's less than {MinTimeToSkip} seconds.");
                } 
                  
                //Update related properties regardless

                _CurrentTime = GoToTime;
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

		public string CurrentChannelTitle { 
			get { return _CurrentChannelTitle;}
			set {
				_CurrentChannelTitle = value;
				OnPropertyChanged("CurrentChannelTitle");
			}
		}

		public bool IsTouched { 
			get { return _IsTouched;}
			set {
				_IsTouched = value;
				OnPropertyChanged("IsTouched");
			}
		}

		public string playerStatus { 
			get { return _playerStatus;}
			set {
				_playerStatus = value;
				OnPropertyChanged("playerStatus");
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

		public event EventHandler PlayerFailure;

		private string stringConvert(double time)
		{ 
			var m = TimeSpan.FromSeconds(time);
			var r = new TimeSpan(m.Days, m.Hours, m.Minutes, m.Seconds, 0);
            if(r.Hours == 0)
			{
				return $"{r.Minutes:D2}:{r.Seconds:D2}";
			}
			else { 
				return $"{r.Hours:D2}:{r.Minutes:D2}:{r.Seconds:D2}";
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		void UpdatePlay()
		{
			AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "play", CurrentTime);
			Task.Run(async () =>
			{
				await AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "play", CurrentTime);
				await Task.Delay(5000);
				ShowWarning = true;
			});
		}

		void UpdatePause()
		{
			var time = CurrentTime >= 0 && CurrentTime < 1.000 ? TotalTime : CurrentTime;
			Task.Run(async () =>
			{
				await AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "pause", time);
				await PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, CurrentTime, RemainingTime);
			});
		}

		async void OnCompleted(object o, EventArgs e)
		{ 
			await PlayerFeedAPI.UpdateEpisodeProperty(Instance.CurrentEpisodeId);
			await AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "stop", TotalTime);
			await PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, 0, stringConvert(TotalTime));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

		private static void NotifyStaticPropertyChanged(string propertyName)
		{
			StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
	}
}
