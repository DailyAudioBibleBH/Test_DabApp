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
			var fd = global::Android.App.Application.Context.Assets.OpenFd(fileName);
			player.Prepared += (s, e) =>
			{
				IsLoaded = true;
			};
			player.Completion += (s, e) =>
			{
				IsLoaded = false;
			};
			player.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
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
			throw new NotImplementedException();
		}

		public bool IsInitialized {
			get { return IsLoaded;}
		}

		public bool IsPlaying {
			get { return player.IsPlaying;}
		}

		public double CurrentTime {
			get { return player.CurrentPosition;}
		}

		public double RemainingTime {
			get { return (player.CurrentPosition - player.Duration);}
		}

		public double TotalTime {
			get { return player.Duration;}
		}

		public List<string> CurrentOutputs
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
}
