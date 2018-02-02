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
            if (AuthenticationAPI.GetTestMode())
            {
                GlobalResources.RestAPIUrl = "https://dailyaudiobibl.staging.wpengine.com/wp-json/lutd/v1/";
                //GlobalResources.FeedAPIUrl = "https://dabfeed.staging.wpengine.com/wp-json/lutd/v1/";
                GlobalResources.TestMode = true;
            }
			InitializeComponent();

			FlowListView.Init();

			if (ContentAPI.CheckContent())
			{
				if (AuthenticationAPI.CheckToken()) {
					MainPage = new NavigationPage(new DabChannelsPage());
				}
				else
				{
					MainPage = new NavigationPage(new DabLoginPage());
				}
			}
			else {
				MainPage = new DabNetworkUnavailablePage();
			}
			MainPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
		}

		protected override void OnStart()
		{
			CrossPushNotification.Current.Register();
		}

		protected override async void OnSleep()
		{
            await AuthenticationAPI.PostActionLogs();
			JournalTracker.Current.Open = false;
		}

		protected override void OnResume()
		{
			JournalTracker.Current.Open = true;
            AuthenticationAPI.GetMemberData();
		}

		//void OnPlayPause(object o, EventArgs e) {
			
		//	if (AudioPlayer.Instance.IsInitialized)
		//	{
		//		if (AudioPlayer.Instance.IsPlaying)
		//		{
		//			AudioPlayer.Instance.Pause();
		//		}
		//		else {
		//			AudioPlayer.Instance.Play();
		//		}
		//	}
		//	else {
		//		AudioPlayer.Instance.SetAudioFile(@"http://www.stephaniequinn.com/Music/Mouret%20-%20Rondeau.mp3");
		//		AudioPlayer.Instance.Play();
		//		}
		//}

		//void OnPodcast(object o, EventArgs e) {
		//	NavigationPage page = (NavigationPage)Application.Current.MainPage;
		//	page.PushAsync(new DabPlayerPage(new dbEpisodes()));

		//}

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
		//			AudioPlayer.Instance.PlayPauseButtonImage = "Play";
		//			return GlobalResources.Player.IsInitialized;
		//		}
		//	});
		//}
	}
}
