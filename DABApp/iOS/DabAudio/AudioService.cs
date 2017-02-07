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

[assembly: Dependency(typeof(AudioService))]
namespace DABApp.iOS
{
	public class AudioService: IAudio
	{
		public static AVPlayer _player;
		public static bool IsLoaded;
		public static AVAudioSession session = AVAudioSession.SharedInstance();

		public AudioService()
		{
		}

		public void SetAudioFile(string fileName)
		{
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
			//_player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
			//{
			//	_player = null;
			//	IsLoaded = false;
			//};
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
