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
					AudioPlayer.Instance.PlayButtonText = "Pause";
				}
			}
			else {
					//GlobalResources.Player.PlayAudioFile("sample.mp3");
				GlobalResources.Player.PlayAudioFile("http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.PlayButtonText = "Pause";
				}
		}

		void OnPodcast(object o, EventArgs e) {
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			page.PushAsync(new DabPlayerView());
		}
	}
}
