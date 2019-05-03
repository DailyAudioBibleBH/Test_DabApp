using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabAudio;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerBar : ContentView
	{
		bool Repeat = true;
        DabPlayer player = GlobalResources.playerPodcast;

		public DabPlayerBar()
		{
			InitializeComponent();

            //PLAYER BINDINGS

            //Visibility of player
            stackPlayerBar.BindingContext = player;
            stackPlayerBar.SetBinding(IsVisibleProperty, "IsReady");

            //Play / Pause button
            PlayerButton.BindingContext = player;
            PlayerButton.SetBinding(Image.SourceProperty, "PlayPauseButtonImageBig");

            //Progress bar (%)
            progProgress.BindingContext = player;
            progProgress.SetBinding(ProgressBar.ProgressProperty, "CurrentProgressPercentage");

            //Episode Title
            lblEpisodeTitle.BindingContext = player;
            lblEpisodeTitle.SetBinding(Label.TextProperty, "EpisodeTitle");

            //Channel Title
            lblChannelTitle.BindingContext = player;
            lblChannelTitle.SetBinding(Label.TextProperty, "ChannelTitle");


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
            //Play or pause the
            if (player.IsPlaying)
            {
                player.Pause();
            }
            else
            {
                player.Play();
            }
        }


		//Show Player Page
		async void OnShowPlayer(object o, EventArgs e)
		{
			if (Repeat)
			{
				Repeat = false;
				PlayerButton.IsEnabled = false;
				stackPodcastTitle.IsEnabled = false;
				NavigationPage page = (NavigationPage)Application.Current.MainPage;
				var currentEpisode = PlayerFeedAPI.GetEpisode(GlobalResources.CurrentEpisodeId);
				var reading = await PlayerFeedAPI.GetReading(currentEpisode.read_link);
				if (Device.Idiom == TargetIdiom.Tablet)
				{
					var channel = ContentConfig.Instance.views.SingleOrDefault(x => x.title == "Channels").resources.SingleOrDefault(r => r.title == currentEpisode.channel_title);
					await page.PushAsync(new DabTabletPage(channel, currentEpisode));
				}
				else
				{
					await page.PushAsync(new DabPlayerPage(currentEpisode, reading));
				}
				stackPodcastTitle.IsEnabled = true;
				PlayerButton.IsEnabled = true;
				Repeat = true;
			}
		}

		//Show share dialog
		void OnShare(object o, EventArgs e)
		{
			var currentEpisode = PlayerFeedAPI.GetEpisode(GlobalResources.CurrentEpisodeId);
			Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(currentEpisode.channel_code, currentEpisode.id.ToString());
		}
	}
}
