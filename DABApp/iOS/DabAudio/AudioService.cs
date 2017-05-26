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
		float seekRate = 10.0f;
		public static AudioService Instance { get; private set; }

		public AudioService()
		{
		}

		static AudioService()
		{
			Instance = new AudioService();
		}

		public void SetAudioFile(string fileName)
		{

			session.SetCategory(AVAudioSession.CategoryPlayback, out error);
			session.SetActive(true);

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

		public void RemoteControlReceived(UIEvent theEvent)
		{
			np = new MPNowPlayingInfo();
			switch (theEvent.Subtype)
			{
				case UIEventSubtype.RemoteControlPause:
					Pause();
					break;
				case UIEventSubtype.RemoteControlPlay:
					Play();
					break;
				case UIEventSubtype.RemoteControlBeginSeekingForward:
					_player.Rate = seekRate;
					np.PlaybackRate = seekRate;
					break;
				case UIEventSubtype.RemoteControlEndSeekingForward:
					_player.Rate = 1.0f;
					np.PlaybackRate = 1.0f;
					break;
				case UIEventSubtype.RemoteControlBeginSeekingBackward:
					_player.Rate = -seekRate;
					np.PlaybackRate = -seekRate;
					break;
				case UIEventSubtype.RemoteControlEndSeekingBackward:
					_player.Rate = 1.0f;
					np.PlaybackRate = 1.0f;
					break;
			}
			np.ElapsedPlaybackTime = _player.CurrentTime.Seconds;
			SetNowPlayingInfo();
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
			np.Title = "Does This Appear?";
			MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = np;
		}

		void SetCommandCenter()
		{
			np = new MPNowPlayingInfo();

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

			Device.StartTimer(new TimeSpan(0, 0, 0, 0, 1), () =>
				{
					SetNowPlayingInfo();
					return true;
				});

		}

		MPRemoteCommandHandlerStatus RemotePlayOrPause(MPRemoteCommandEvent arg)
		{
			if (IsPlaying)
			{
				Pause();
				np.PlaybackRate = 0f;
			}
			else {
				Play();
				np.PlaybackRate = 1.0f;
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
	}
}
