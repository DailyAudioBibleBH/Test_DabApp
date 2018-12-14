using System;
using Xamarin.Forms;
using DABApp;
using DABApp.iOS;
using System.IO;
using Foundation;
using AVFoundation;
using CoreMedia;
using System.Collections.Generic;
using System.Linq;
using AudioToolbox;
using UIKit;
using MediaPlayer;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using Plugin.Connectivity;

[assembly: Dependency(typeof(AudioService))]
namespace DABApp.iOS
{
	public class AudioService : IAudio
	{
		public static AVPlayer _player;
		public static bool IsLoaded;
		MPNowPlayingInfo np;
		MPRemoteCommandCenter commandCenter = MPRemoteCommandCenter.Shared;
		AVAudioSession session = AVAudioSession.SharedInstance();
		NSError error;
		double skipInterval = 30;
        //float seekRate = 10.0f;
		public static AudioService Instance { get; private set; }
		dbEpisodes CurrentEpisode;
        bool ableToKeepUp;
        static bool UpdateOnPlay = false;

		public AudioService()
		{
		}

		static AudioService()
		{
			Instance = new AudioService();
		}

        public void SetAudioFile(string fileName)
        {
            session.SetCategory(AVAudioSession.CategoryPlayback, AVAudioSessionCategoryOptions.AllowAirPlay, out error);
            session.SetActive(true);
            _player = AVPlayer.FromUrl(NSUrl.FromFilename(fileName));
            IsLoaded = true;
        }

		public void SetAudioFile(string fileName, dbEpisodes episode)
		{
            
            ableToKeepUp = true;
            UpdateOnPlay = false;
			CurrentEpisode = episode == null ? CurrentEpisode : episode;
			session.SetCategory(AVAudioSession.CategoryPlayback, AVAudioSessionCategoryOptions.AllowAirPlay, out error);
			session.SetActive(true);
			AVAudioSession.Notifications.ObserveInterruption((sender, args) => {
				if (args.InterruptionType == AVAudioSessionInterruptionType.Ended && args.Option == AVAudioSessionInterruptionOptions.ShouldResume)
				{
					Play();
				}
				});
			nint TaskId = 0;
			TaskId = UIApplication.SharedApplication.BeginBackgroundTask(delegate
			{
				if (TaskId != 0)
				{
					UIApplication.SharedApplication.EndBackgroundTask(TaskId);
					TaskId = 0;
					SetNowPlayingInfo();
				}
			});

            var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName = Path.Combine(doc, fileName);
            
            //if (!episode.is_downloaded && CrossConnectivity.Current.IsConnected)
            if(!episode.is_downloaded && CrossConnectivity.Current.IsConnected)
			{
                NSUrl url = NSUrl.FromString(episode.url);
                _player = AVPlayer.FromUrl(url);
            }
			else
			{
				var url = NSUrl.FromFilename(fileName);
                Debug.WriteLine(url.AbsoluteString);
				_player = AVPlayer.FromUrl(url);
			}
            if (!episode.is_downloaded && File.Exists(fileName))
            {
                FileManagement.DoneDownloading += DoneDownloading;
            }
            _player.ActionAtItemEnd = AVPlayerActionAtItemEnd.Pause;
            NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.ItemFailedToPlayToEndTimeNotification, (notification) => {
                Pause();
                if (!UpdateOnPlay)
                {
                    PlayerCanKeepUp = false;
                }
                else Play();
            });
            NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.PlaybackStalledNotification, (notification) => {
                Pause();
                if (UpdateOnPlay)
                    Play();
            });
			SetCommandCenter();
			IsLoaded = true;
		}

        private void DoneDownloading(object sender, DabEventArgs e)
        {
            if (e.EpisodeId == CurrentEpisode.id.Value)
            {
                UpdateOnPlay = true;
            }
        }

        public void Play()
		{
            if (UpdateOnPlay)
            {
                var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                NSUrl url = NSUrl.FromFilename(Path.Combine(doc, $"{CurrentEpisode.id.Value.ToString()}.{CurrentEpisode.url.Split('.').Last()}"));
                var currentTime = _player.CurrentTime;
                Debug.WriteLine(url.AbsoluteString);
                _player.ReplaceCurrentItemWithPlayerItem(AVPlayerItem.FromAsset(AVAsset.FromUrl(url)));//Updating currently playing file so it isn't partial anymore
                _player.Seek(currentTime);
                UpdateOnPlay = false;
                PlayerCanKeepUp = true;
            }
            _player.Play();
		}

		public void Pause()
		{
            if (_player != null)
            {
                _player.Pause();
            }
        }

		public void SeekTo(int seconds)
		{
			_player.Seek(new CMTime(seconds, 1), CMTime.Zero, CMTime.Zero);
		}

		public void Skip(int seconds)
		{
			int seekTime = Convert.ToInt32(_player.CurrentTime.Seconds) + seconds;
			_player.Seek(new CMTime(seekTime, 1), CMTime.Zero, CMTime.Zero);
		}

		public void SwitchOutputs()
		{
			if (session.CurrentRoute.Outputs[0].PortName == "Speaker")
			{
				session.OverrideOutputAudioPort(AVAudioSessionPortOverride.None, out error);
			}
			else
			{
				session.OverrideOutputAudioPort(AVAudioSessionPortOverride.Speaker, out error);
			}
			var n = error;
		}

        public void DeCouple()
        {
            if (_player != null)
            {
                IsLoaded = false;
                _player = new AVPlayer();
                MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = new MPNowPlayingInfo();
            }
        }

		void SetNowPlayingInfo()
		{
			//np = new MPNowPlayingInfo();
			if (np.ElapsedPlaybackTime != _player.CurrentTime.Seconds)
			{
				np.ElapsedPlaybackTime = _player.CurrentTime.Seconds;
			}
			if (np.PlaybackDuration != _player.CurrentItem.Duration.Seconds)
			{
				np.PlaybackDuration = _player.CurrentItem.Duration.Seconds;
			}
			np.Title = CurrentEpisode.title;
			//np.Artwork = new MPMediaItemArtwork(await LoadImage(ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == CurrentEpisode.channel_title).images.thumbnail));
			MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = np;
		}

		async Task<UIImage> LoadImage(string imageUrl)
		{
			var client = new HttpClient();
			Task<byte[]> contentsTask = client.GetByteArrayAsync(imageUrl);
			var contents = await contentsTask;
			return UIImage.LoadFromData(NSData.FromArray(contents));
		}

		async Task SetCommandCenter()
		{
			np = new MPNowPlayingInfo();
			try
			{
				np.Artwork = new MPMediaItemArtwork(await LoadImage(ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == CurrentEpisode.channel_title).images.thumbnail));
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Exception thrown setting artwork of media item: {ex.Message}");
			}
			MPSkipIntervalCommand skipForward = commandCenter.SkipForwardCommand;
			skipForward.Enabled = true;
			skipForward.AddTarget(RemoteSkip);
			skipForward.PreferredIntervals = new double[1] { skipInterval };
			MPSkipIntervalCommand skipBackward = commandCenter.SkipBackwardCommand;
			skipBackward.Enabled = true;
			skipBackward.AddTarget(RemoteSkip);
			skipBackward.PreferredIntervals = new double[1] { -skipInterval };
			MPRemoteCommand pauseCommand = commandCenter.PauseCommand;
			pauseCommand.Enabled = true;
			pauseCommand.AddTarget(RemotePlayOrPause);
			MPRemoteCommand playCommand = commandCenter.PlayCommand;
			playCommand.Enabled = true;
			playCommand.AddTarget(RemotePlayOrPause);
			MPRemoteCommand playpauseCommand = commandCenter.TogglePlayPauseCommand;
			playpauseCommand.Enabled = true;
			playpauseCommand.AddTarget(RemotePlayOrPause);

			Device.StartTimer(new TimeSpan(0, 0, 0, 0, 1), () => 
				{
                    if (!IsLoaded)
                    {
                        return false;
                    }
					SetNowPlayingInfo();
					return true;
				});

		}

		MPRemoteCommandHandlerStatus RemotePlayOrPause(MPRemoteCommandEvent arg)
		{
			if (arg.Command == commandCenter.PauseCommand)
			{
				Pause();
				np.PlaybackRate = 0f;
			}
			if (arg.Command == commandCenter.PlayCommand)
			{
				Play();
				np.PlaybackRate = 1.0f;
			}
			if (arg.Command == commandCenter.TogglePlayPauseCommand)
			{
				if (_player.Rate != 0f)
				{
					Pause();
					np.PlaybackRate = 0f;
				}
				else
				{
					Play();
					np.PlaybackRate = 1.0f;
				}
			}
			return MPRemoteCommandHandlerStatus.Success;
		}

		MPRemoteCommandHandlerStatus RemoteSkip(MPRemoteCommandEvent arg)
		{
			if (arg.Command == commandCenter.SkipBackwardCommand)
			{
				Skip((int)-skipInterval);
			}
			if (arg.Command == commandCenter.SkipForwardCommand)
			{
				Skip((int)skipInterval);
			}
			return MPRemoteCommandHandlerStatus.Success;
		}

		public void Unload()
		{
			IsLoaded = false;
		}

		public bool IsInitialized
		{
			get { return IsLoaded; }
		}

		public bool IsPlaying
		{
			get
			{
				if (_player != null)
				{
					if (_player.Rate != 0 && _player.Error == null)
					{
						return true;
					}
					else {
						return false;
					}
				}
				else {
					return false;
				}
			}
		}

		public double CurrentTime
		{
			get
			{
				if (_player != null)
				{

					return _player.CurrentTime.Seconds;
				}
				else
				{
					return 0;
				}
			}
		}


		public double TotalTime
		{
			get
			{
				if (_player != null)
				{
					return _player.CurrentItem.Duration.Seconds;
				}
				else
				{
					return 0;
				}
			}
		}

		public bool PlayerCanKeepUp
		{ 
			get 
			{
				if (_player != null)
				{
					return ableToKeepUp;
				}
				else return false;
			}
            set {
                ableToKeepUp = value;
            }
		}

		public event EventHandler Completed;
	}
}
