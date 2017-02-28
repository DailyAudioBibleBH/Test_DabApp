using System;
using System.Collections.Generic;
using PushNotification.Plugin;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;

namespace DABApp
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			FlowListView.Init();

			if (ContentAPI.CheckContent())
			{
				MainPage = new NavigationPage(new DabChannelsPage());
			}
			else {
				MainPage = new DabNetworkUnavailablePage();
			}
			//AudioPlayer.Instance.Player.SetAudioFile(@"http://www.stephaniequinn.com/Music/Mouret%20-%20Rondeau.mp3");
		}

		protected override void OnStart()
		{
			CrossPushNotification.Current.Register();
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
			
			if (AudioPlayer.Instance.Player.IsInitialized)
			{
				if (AudioPlayer.Instance.Player.IsPlaying)
				{
					AudioPlayer.Instance.Player.Pause();
					AudioPlayer.Instance.PlayButtonText = "Play";
				}
				else {
					AudioPlayer.Instance.Player.Play();
					//ProgressBinding();
					AudioPlayer.Instance.PlayButtonText = "Pause";
				}
			}
			else {
				//AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.Player.SetAudioFile(@"http://www.stephaniequinn.com/Music/Mouret%20-%20Rondeau.mp3");
				AudioPlayer.Instance.Player.Play();
				//ProgressBinding();
				AudioPlayer.Instance.PlayButtonText = "Pause";
				}
		}

		void OnPodcast(object o, EventArgs e) {
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			page.PushAsync(new DabPlayerPage());

		}

		//void ProgressBinding() {
		//	Device.StartTimer(new TimeSpan(0, 0, 1), () =>
		//	{
		//		if (GlobalResources.Player.IsInitialized)
		//		{
		//			AudioPlayer.Instance.Progress = (GlobalResources.Player.CurrentTime / GlobalResources.Player.TotalTime);
		//			return GlobalResources.Player.IsPlaying;
		//		}
		//		else {
		//			AudioPlayer.Instance.Progress = 0;
		//			AudioPlayer.Instance.PlayButtonText = "Play";
		//			return GlobalResources.Player.IsInitialized;
		//		}
		//	});
		//}
	}
}
