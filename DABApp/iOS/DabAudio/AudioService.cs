﻿using System;
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
	public class AudioService: IAudio
	{
		public static AVPlayer _player;
		public static bool IsLoaded;
		MPNowPlayingInfo np;
		AVAudioSession session = AVAudioSession.SharedInstance();
		NSError error;
		float seekRate = 10.0f;

		public AudioService()
		{
		}

		public void SetAudioFile(string fileName)
		{
			session.SetCategory(AVAudioSession.CategoryPlayback, out error);
			session.SetActive(true);
			UIApplication.SharedApplication.BeginReceivingRemoteControlEvents();

			nint TaskId = 0;
			TaskId = UIApplication.SharedApplication.BeginBackgroundTask(delegate
			{
				if (TaskId != 0) {
					UIApplication.SharedApplication.EndBackgroundTask(TaskId);
					TaskId = 0;
					SetNowPlayingInfo();
				}
			});

			if (fileName.Contains("http://"))
			{
				NSUrl url = NSUrl.FromString(fileName);
				_player = AVPlayer.FromUrl(url);
			}
			else
			{
				string sFilePath = NSBundle.MainBundle.PathForResource
				(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
				var url = NSUrl.FromFilename(sFilePath);
				_player = AVPlayer.FromUrl(url);
			}
			IsLoaded = true;
		}

		public void Play() {
			_player.Play();
		}

		public void Pause() {
			_player.Pause();
		}

		public void SeekTo(int seconds) {
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
			switch (theEvent.Subtype) {
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

		void SetNowPlayingInfo() { 
			np = new MPNowPlayingInfo();
			np.ElapsedPlaybackTime = _player.CurrentTime.Seconds;
			np.PlaybackDuration = _player.CurrentItem.Duration.Seconds;
			MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = np;
		}

		public bool IsInitialized {
			get { return IsLoaded;}
		}

		public bool IsPlaying {
			get {
				if (_player.Rate != 0 && _player.Error == null)
				{
					return true;
				}
				else return false;
			}
		}

		public double CurrentTime {
			get { return _player.CurrentTime.Seconds;}
		}

		//public double RemainingTime
		//{
		//	get {return (_player.CurrentTime.Seconds - _player.CurrentItem.Duration.Seconds);}
		//}

		public double TotalTime {
			get { return _player.CurrentItem.Duration.Seconds;}
		}
	}
}
