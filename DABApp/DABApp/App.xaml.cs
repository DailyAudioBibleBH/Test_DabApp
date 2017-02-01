using System;
using System.Collections.Generic;
using Xamarin.Forms;


namespace DABApp
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
			MainPage = new NavigationPage(new DabChannelsPage())
			{
				BarTextColor = Color.White,
				BarBackgroundColor = Color.Black
			};
			GlobalResources.Player = DependencyService.Get<IAudio>();
			GlobalResources.Player.SetAudioFile("sample.mp3");
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}

		void OnPlayPause(object o, EventArgs e) {
			if (GlobalResources.Player.IsInitialized())
			{
				if (GlobalResources.Player.IsPlaying())
				{
					GlobalResources.Player.Pause();
					AudioPlayer.Instance.PlayButtonText = "Play";
				}
				else {
					GlobalResources.Player.Play();
					ProgressBinding();
					AudioPlayer.Instance.PlayButtonText = "Pause";
				}
			}
			else {
				GlobalResources.Player.SetAudioFile("sample.mp3");
				//GlobalResources.Player.PlayAudioFile("http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				GlobalResources.Player.Play();
				ProgressBinding();
				AudioPlayer.Instance.PlayButtonText = "Pause";
				}
		}

		void OnPodcast(object o, EventArgs e) {
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			page.PushAsync(new DabPlayerView());
		}

		void ProgressBinding() {
			Device.StartTimer(new TimeSpan(0, 0, 1), () =>
			{
				if (GlobalResources.Player.IsInitialized())
				{
					AudioPlayer.Instance.Progress = (GlobalResources.Player.CurrentTime() / GlobalResources.Player.TotalTime());
					return GlobalResources.Player.IsPlaying();
				}
				else {
					AudioPlayer.Instance.Progress = 0;
					AudioPlayer.Instance.PlayButtonText = "Play";
					return GlobalResources.Player.IsInitialized();
				}
			});
		}
	}
}
