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
		public AudioService()
		{
		}

		public void PlayAudioFile(string fileName)
		{
			//if (fileName.Contains("http://"))
			//{

			//	NSUrl url = NSUrl.FromString(fileName);
			//	AVAsset asset = AVAsset.FromUrl(url);
			//	AVPlayerItem item = AVPlayerItem.FromAsset(asset);
			//	var _player = AVAudioPlayer.FromObject(item);
			//	_player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
			//	{
			//		_player = null;
			//	};
			//	_player.Play();
			//}
			//else
			//{
				string sFilePath = NSBundle.MainBundle.PathForResource
				(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
				var url = NSUrl.FromString(sFilePath);
				var _player = AVAudioPlayer.FromUrl(url);
				_player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
				{
					_player = null;
				};
				_player.Play();
			//}
		}
	}
}
