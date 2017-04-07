using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerBar : ContentView
	{
		public DabPlayerBar()
		{
			InitializeComponent();
		}

		//Play - Pause
		void OnPlayPause(object o, EventArgs e)
		{

			if (AudioPlayer.Instance.IsInitialized)
			{
				if (AudioPlayer.Instance.IsPlaying)
				{
					AudioPlayer.Instance.Pause();
				}
				else {
					AudioPlayer.Instance.Play();
				}
			}
			else {
				AudioPlayer.Instance.Play();
			}
		}

		//Show Player Page
		void OnShowPlayer(object o, EventArgs e)
		{
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			var currentEpisode = PlayerFeedAPI.GetEpisode(AudioPlayer.Instance.CurrentEpisodeId);
			page.PushAsync(new DabPlayerPage(currentEpisode));

		}

		//Show share dialog
		void OnShare(object o, EventArgs e)
		{
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			page.DisplayAlert("Share episode", "This button will share this episode.", "OK");
		}
	}
}
