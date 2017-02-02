using System;
using Xamarin.Forms;
using DABApp;
using DABApp.iOS;
using System.IO;
using Foundation;
using AVFoundation;

[assembly: Dependency(typeof(AudioService))]
namespace DABApp.iOS
{
	public class AudioService: IAudio
	{
		public static AVAudioPlayer _player;
		public static bool IsLoaded;

		public AudioService()
		{
		}

		public void SetAudioFile(string fileName)
		{
			if (fileName.Contains("http://"))
			{
				NSUrl url = NSUrl.FromString(fileName);
				NSData data = NSData.FromUrl(url);
				_player = AVAudioPlayer.FromData(data);
			}
			else
			{
				string sFilePath = NSBundle.MainBundle.PathForResource
				(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
				var url = NSUrl.FromString(sFilePath);
				_player = AVAudioPlayer.FromUrl(url);
			}
			_player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
			{
				_player = null;
				IsLoaded = false;
			};
			IsLoaded = true;
		}

		public void Play() {
			_player.Play();
		}

		public void Pause() {
			_player.Pause();
		}

		public void SeekTo(int seconds) {
			_player.CurrentTime += seconds;
		}

		public bool IsInitialized {
			get { return IsLoaded;}
		}

		public bool IsPlaying {
			get { return _player.Playing;}
		}

		public double CurrentTime {
			get { return _player.CurrentTime;}
		}

		public double RemainingTime
		{
			get {return (_player.CurrentTime - _player.Duration);}
		}

		public double TotalTime {
			get { return _player.Duration;}
		}
	}
}
