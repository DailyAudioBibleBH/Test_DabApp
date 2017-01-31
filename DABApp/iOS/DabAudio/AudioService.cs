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

		public AudioService()
		{
		}

		public void PlayAudioFile(string fileName)
		{
			if (fileName.Contains("http://"))
			{

				NSUrl url = NSUrl.FromString(fileName);
				var _player = AVPlayer.FromUrl(url);
				_player.Play();
			}
			else
			{
				string sFilePath = NSBundle.MainBundle.PathForResource
				(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
				var url = NSUrl.FromString(sFilePath);
				_player = AVAudioPlayer.FromUrl(url);
				_player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
				{
					_player = null;
				};
				_player.Play();
			}
		}
	}
}
