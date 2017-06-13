using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerBar : ContentView
	{
		public DabPlayerBar()
		{
			InitializeComponent();


			//Add a tap recognizer for the podcast tit
			var tapShowEpisode = new TapGestureRecognizer();
			tapShowEpisode.NumberOfTapsRequired = 1;
			tapShowEpisode.Tapped += (sender, e) =>
			{
				OnShowPlayer(sender, e);
			};
			stackPlayerBar.GestureRecognizers.Add(tapShowEpisode);

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
			PlayerButton.IsEnabled = false;
			stackPodcastTitle.IsEnabled = false;
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			var currentEpisode = PlayerFeedAPI.GetEpisode(AudioPlayer.Instance.CurrentEpisodeId);
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				var channel = ContentConfig.Instance.views.SingleOrDefault(x => x.title == "Channels").resources.SingleOrDefault(r => r.title == currentEpisode.channel_title);
				page.PushAsync(new DabTabletPage(channel, currentEpisode));
			}
			else
			{
				page.PushAsync(new DabPlayerPage(currentEpisode));
			}
			stackPodcastTitle.IsEnabled = true;
			PlayerButton.IsEnabled = true;
		}

		//Show share dialog
		void OnShare(object o, EventArgs e)
		{
			var currentEpisode = PlayerFeedAPI.GetEpisode(AudioPlayer.Instance.CurrentEpisodeId);
			Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(currentEpisode.channel_code, currentEpisode.id.ToString());
		}
	}
}
