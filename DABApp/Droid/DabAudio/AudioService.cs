using System;
using DABApp.Droid;
using Xamarin.Forms;
using Android.Media;
using Android.Content.Res;

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

		public bool IsInitialized() {
			return IsLoaded;
		}

		public bool IsPlaying() {
			return player.IsPlaying;
		}

		public double CurrentTime() {
			return player.CurrentPosition;
		}

		public double RemainingTime() {
			return player.CurrentPosition - player.Duration;
		}

		public double TotalTime() {
			return player.Duration;
		}
	}
}
