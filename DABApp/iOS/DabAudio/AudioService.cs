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

		public AudioService()
		{
		}

		static AudioService()
		{
			Instance = new AudioService();
		}

		public void SetAudioFile(string fileName, dbEpisodes episode)
		{
			CurrentEpisode = episode;
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

			if (fileName.Contains("http://") || fileName.Contains("https://"))
			{
				NSUrl url = NSUrl.FromString(fileName);
				_player = AVPlayer.FromUrl(url);
			}
			else
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				fileName = Path.Combine(doc, fileName);
				//string sFilePath = NSBundle.MainBundle.PathForResource
				//(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
				var url = NSUrl.FromFilename(fileName);
				_player = AVPlayer.FromUrl(url);
			}
			_player.ActionAtItemEnd = AVPlayerActionAtItemEnd.Pause;
            NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.PlaybackStalledNotification, (notification) => { SetAudioFile(fileName, episode); });
			SetCommandCenter();
			IsLoaded = true;
		}

		public void Play()
		{
			_player.Play();
		}

		public void Pause()
		{
			_player.Pause();
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
					return _player.CurrentItem.PlaybackLikelyToKeepUp;
				}
				else return false;
			}
		}

		public event EventHandler Completed;
	}
}
