using System;
using DABApp.Droid;
using Xamarin.Forms;
using Android.Media;
using Android.Content.Res;
using System.Collections.Generic;

[assembly: Dependency(typeof(AudioService))]
namespace DABApp.Droid
{
	public class AudioService: IAudio
	{
		public static MediaPlayer player;
		public static bool IsLoaded;

		public AudioService()
		{
		}

		public void SetAudioFile(string fileName)
		{
			player = new MediaPlayer();
			player.Prepared += (s, e) =>
			{
				IsLoaded = true;
			};
			player.Completion += (s, e) =>
			{
				IsLoaded = false;
			};
			if (fileName.Contains("http"))
			{
				player.SetDataSource(fileName);
			}
			else {
				var fd = global::Android.App.Application.Context.Assets.OpenFd(fileName);
				player.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
			}
			player.Prepare();
		}

		public void Play() {
			player.Start();
		}

		public void Pause() {
			player.Pause();
		}

		public void SeekTo(int seconds) {
			player.SeekTo(seconds * 1000);
		}

		public void Skip(int seconds)
		{
			player.SeekTo((Convert.ToInt32(CurrentTime) + seconds) * 1000);
		}

		public void Unload()
		{
			throw new NotImplementedException();
		}

		public bool IsInitialized {
			get { return IsLoaded;}
		}

		public bool IsPlaying {
			get { return player.IsPlaying;}
		}

		public double CurrentTime {
			get { return player.CurrentPosition/1000;}
		}

		public double RemainingTime {
			get { return (player.CurrentPosition - player.Duration)/1000;}
		}

		public double TotalTime {
			get { return player.Duration/1000;}
		}

	}
}
