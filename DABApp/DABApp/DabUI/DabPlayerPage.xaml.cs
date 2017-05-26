using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerPage : DabBaseContentPage
	{
		//IAudio player = GlobalResources.Player;
		dbEpisodes Episode;
		string backgroundImage;

		public DabPlayerPage(dbEpisodes episode)
		{
			InitializeComponent();

			Episode = episode;
			//AudioPlayer.Instance.ShowPlayerBar = false;
			SeekBar.Value = AudioPlayer.Instance.CurrentTime;
			DabViewHelper.InitDabForm(this);
			backgroundImage = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == episode.channel_title).images.backgroundPhone;
			BackgroundImage.Source = backgroundImage;
			BindingContext = episode;
			//Date.Text = $"{episode.PubMonth} {episode.PubDay.ToString()} {episode.PubYear.ToString()}";
			base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			Reading reading = PlayerFeedAPI.GetReading(episode.read_link);
			ReadTitle.Text = reading.title;
			ReadText.Text = reading.text;
			if (reading.excerpts != null)
			{
				ReadExcerpts.Text = String.Join(", ", reading.excerpts);
			}
			var tapper = new TapGestureRecognizer();
			tapper.NumberOfTapsRequired = 1;
			tapper.Tapped += (sender, e) =>
			{
				Navigation.PushModalAsync(new NavigationPage(new DabSignUpPage()));
			};
			SignUp.GestureRecognizers.Add(tapper);
			SignUp.Text = "<div style='font-size:15px;'>Don't have an account? <font color='#ff0000'>Sign Up</font></div>";
		}

		void OnPlay(object o, EventArgs e)
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
				AudioPlayer.Instance.SetAudioFile(Episode);
				AudioPlayer.Instance.Play();
			}
		}

		void OnBack30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Skip(-30);
		}

		void OnForward30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Skip(30);
		}

		void Handle_ValueChanged(object sender, System.EventArgs e)
		{
			switch (SegControl.SelectedSegment)
			{
				case 0:
					Read.IsVisible = false;
					if (GlobalResources.IsGuestLogin)
					{
						LoginJournal.IsVisible = false;
					}
					else
					{
						Journal.IsVisible = false;
					}
					//AudioPlayer.Instance.showPlayerBar = false;
					Listen.IsVisible = true;
					BackgroundImage.IsVisible = true;
					break;
				case 1:
					Listen.IsVisible = false;
					if (GlobalResources.IsGuestLogin) {
						LoginJournal.IsVisible = false;
					}
					else
					{
						Journal.IsVisible = false;
					}
					//AudioPlayer.Instance.showPlayerBar = true;
					Read.IsVisible = true;
					BackgroundImage.IsVisible = false;
					break;
				case 2:
					Read.IsVisible = false;
					Listen.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = true;
					if (GlobalResources.IsGuestLogin)
					{
						LoginJournal.IsVisible = true;
					}
					else
					{
						Journal.IsVisible = true;
					}
					BackgroundImage.IsVisible = false;
					break;
			}
		}

		//void OnPlayPause(object o, EventArgs e)
		//{

		//	if (AudioPlayer.Instance.Player.IsInitialized)
		//	{
		//		if (AudioPlayer.Instance.Player.IsPlaying)
		//		{
		//			AudioPlayer.Instance.Player.Pause();
		//			AudioPlayer.Instance.PlayPauseButtonImage = playImage;
		//		}
		//		else {
		//			AudioPlayer.Instance.Player.Play();
		//			//ProgressBinding();
		//			AudioPlayer.Instance.PlayPauseButtonImage = pauseImage;
		//		}
		//	}
		//	else {
		//		//AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
		//		AudioPlayer.Instance.Player.SetAudioFile("@"+Episode.url);
		//		AudioPlayer.Instance.Player.Play();
		//		//ProgressBinding();
		//		AudioPlayer.Instance.PlayPauseButtonImage = pauseImage;
		//	}
		//}

		void OnPodcast(object o, EventArgs e)
		{
			SegControl.SelectTab(0);
		}

		void OnSaveJournal(object o, EventArgs e) { 
			
		}

		void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
			Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			Login.IsEnabled = true;
		}
	}
}
