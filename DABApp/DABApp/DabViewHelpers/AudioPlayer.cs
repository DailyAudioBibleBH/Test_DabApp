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
        private bool onRecord = false;
        public dbEpisodes CurrentEpisode { get; set; }
        double dura;
        

		// Singleton for use throughout the app
		public static AudioPlayer Instance { get; private set; }
        public static AudioPlayer RecordingInstance { get; private set; }
        public double MinTimeToSkip { get; set; } = 5;
        static AudioPlayer()
		{
			Instance = new AudioPlayer();
            RecordingInstance = new AudioPlayer();
            Instance.OnRecord = false;
            RecordingInstance.OnRecord = true;
		}

		//Don't allow creation of the class elsewhere in the app.
		private AudioPlayer()
		{
			//Create a player object 
			Player = DependencyService.Get<IAudio>(DependencyFetchTarget.NewInstance);
			//_player.Completed += OnCompleted;
			// Start a timer to get time information from the player
            //20171107 Increased from 100 to 1000 to help with skipping
			Device.StartTimer(TimeSpan.FromSeconds(1), () =>
						{
                            //IsInitialized = Player.IsInitialized != IsInitialized ? Player.IsInitialized : IsInitialized;
							if (Player.IsInitialized)
							{
								//Update current time
								if (_CurrentTime != Player.CurrentTime)
								{
									if (_CurrentTime < 0)
									{
										_CurrentTime = 0;
									}
									CurrentTime = Player.CurrentTime;
									if (!Double.IsNaN(Player.TotalTime))
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
								Debug.WriteLine($"CurrentTime = {CurrentTime}, TotalTime = {Player.TotalTime}");
											CurrentTime = 0;
											RemainingTime = stringConvert(TotalTime);
											Debug.WriteLine($"RemainingTime = {RemainingTime}");
											Pause();
							}
									}
								}

								if (_TotalTime != Player.TotalTime && !Double.IsNaN(Player.TotalTime))
								{
									TotalTime = Player.TotalTime;
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
                                    OnPropertyChanged("PlayPauseAccessible");
                                    if (!OnRecord)//This is always false for Instance so that the RecordingInstance doesn't update the play position of the episode in the database
                                    {
                                        if (IsPlaying)
                                        {
                                            UpdatePlay();
                                        }
                                        else UpdatePause();
                                    }
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

								if (!Player.PlayerCanKeepUp && ShowWarning)
								{

                                    PlayerFailure?.Invoke(this, new EventArgs());
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
        private IAudio Player { get; }


        //property for whether or not a file is loaded (and whether to display the player bar)
        public bool IsInitialized
		{
			get
			{
				return _IsInitialized;
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
			Player.SetAudioFile(FileName);
            if (Device.RuntimePlatform == Device.Android)
            {
                Player.Pause();
            }
            ShowWarning = false;
		}

		public void SetAudioFile(dbEpisodes episode)
		{
            //episode = OnRecord && CurrentEpisode != null ? CurrentEpisode : episode;
            bool wasPlaying = Player.IsPlaying;//Setting these values in memory so that they don't change when the AudioPlayer gets updated
            var time = CurrentTime;
            var id = CurrentEpisodeId;
            var rtime = RemainingTime;
            CurrentEpisode = episode;
            CurrentEpisodeId = (int)episode.id;
			CurrentEpisodeTitle = episode.title;
			CurrentChannelTitle = episode.channel_title;
			var ext = episode.url.Split('.').Last();
			Player.SetAudioFile($"{episode.id.ToString()}.{ext}", episode);
			//if (episode.stop_time < _player.TotalTime || Device.RuntimePlatform == "Android")
			//{
				Debug.WriteLine($"episode.stop_time = {episode.stop_time}");
				CurrentTime = episode.stop_time;
            //TotalTime = OnRecord && dura != 0 ? dura : 1;
			//}
			//else
			//{
			//	CurrentTime = 0;
			//}
			Debug.WriteLine($"episode.remaining_time = {episode.remaining_time}");
			RemainingTime = episode.remaining_time;
			ShowWarning = Device.RuntimePlatform == "iOS" ? false: true;
            if (wasPlaying)
            {
                Task.Run(async () =>
                {
                    await PlayerFeedAPI.UpdateStopTime(id, time, rtime);
                    await AuthenticationAPI.CreateNewActionLog(id, "pause", time, null);
                });
            }
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

        public string PlayPauseAccessible
        {
            get
            {
                if (IsPlaying)
                {
                    return "Pause button";
                }
                else
                {
                    return "Play button";
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
				Player.Play();
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
				Player.Pause();
				OnPropertyChanged("PlayPauseButtonImage");
				OnPropertyChanged("PlayPauseButtonImageBig");
				//UpdatePause();
			}
		}

		public void Unload(){
			Player.Unload();
		}

		//property for whether a file is being played or not
		public bool IsPlaying
		{
			get
			{
				return Player.IsPlaying;
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
                if (value != 1) //ignore 1 - this is the default when the player page is initialized and "never" a real value.
                {
                    double GoToTime = value;
                    double PlayerTime = Player.CurrentTime;
                    if (Math.Abs((GoToTime - PlayerTime)) > MinTimeToSkip)
                    {
						Debug.WriteLine($"Seekto Time = {GoToTime}");
						Player.SeekTo(Convert.ToInt32(GoToTime));
                    }
                    //else
                    //{
                    //    Debug.WriteLine($"Ignoring current time change from {PlayerTime} to {GoToTime} because it's less than {MinTimeToSkip} seconds.");
                    //}
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

        public bool OnRecord
        {
            get
            {
                return onRecord;
            }
            set
            {
                onRecord = value;
                Player.OnRecord = onRecord;
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
			Debug.WriteLine($"SeekTo seconds = {seconds}");
			Player.SeekTo(seconds);
			//Update the current time
			CurrentTime = seconds;

		}

		public void Skip(int seconds)
		{
			Debug.WriteLine($"Skip = {seconds}");
			Player.Skip(seconds);
			//Update the current time
			//CurrentTime = seconds;
		}

		public void GetOutputs() {
			Player.SwitchOutputs();
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
			Task.Run(async () => { await AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "play", CurrentTime, null); });
			Task.Run(async () =>
			{
				await AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "play", CurrentTime, null);
				await Task.Delay(5000);
				ShowWarning = true;
			});
		}

		void UpdatePause()
		{
            //Storing these values in memory separately so that they don't change by the time they're called by the async functions
			var time = CurrentTime;
            var id = CurrentEpisodeId;
            var rtime = RemainingTime;
			Task.Run(async () =>
			{
				await AuthenticationAPI.CreateNewActionLog(id, "pause", time, null);
				await PlayerFeedAPI.UpdateStopTime(id, time, rtime);
			});
		}

		async void OnCompleted(object o, EventArgs e)
		{ 
			await PlayerFeedAPI.UpdateEpisodeProperty(Instance.CurrentEpisodeId);
			await AuthenticationAPI.CreateNewActionLog(CurrentEpisodeId, "stop", TotalTime, null);
			await PlayerFeedAPI.UpdateStopTime(CurrentEpisodeId, 0, stringConvert(TotalTime));
		}

        //public void DeCouple()
        //{
        //    Player.Pause();
        //    IsPlaying = false;
        //    if (!OnRecord && CurrentEpisode != null)
        //    {
        //        CurrentEpisode.start_time = Player.CurrentTime;
        //        CurrentEpisode.stop_time = Player.CurrentTime;
        //        CurrentEpisode.remaining_time = RemainingTime;
        //        dura = Player.TotalTime;
        //    }
        //    Player.DeCouple();
        //}

		public event PropertyChangedEventHandler PropertyChanged;

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

		private static void NotifyStaticPropertyChanged(string propertyName)
		{
			StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
	}
}
