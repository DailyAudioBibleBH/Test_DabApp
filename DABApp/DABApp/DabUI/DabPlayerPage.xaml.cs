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
		bool IsGuest;

		public DabPlayerPage(dbEpisodes episode)
		{
			InitializeComponent();
			if (episode.id != AudioPlayer.Instance.CurrentEpisodeId) {
				SeekBar.IsVisible = false;
				TimeStrings.IsVisible = false;
				PlayerControls.IsVisible = false;
				Initializer.IsVisible = true;
			}
			IsGuest = GuestStatus.Current.IsGuestLogin;
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
			if (reading.IsAlt)
			{
				AltWarning.IsVisible = true;
			}
			else AltWarning.IsVisible = false;
			if (reading.excerpts != null)
			{
				ReadExcerpts.Text = String.Join(", ", reading.excerpts);
			}
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
					if (GuestStatus.Current.IsGuestLogin)
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
					if (GuestStatus.Current.IsGuestLogin) {
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
					if (GuestStatus.Current.IsGuestLogin)
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
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			Navigation.PushModalAsync(new NavigationPage(new DabLoginPage(true)));
			Login.IsEnabled = true;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (LoginJournal.IsVisible || Journal.IsVisible) {
				if (GuestStatus.Current.IsGuestLogin)
				{
					LoginJournal.IsVisible = true;
					Journal.IsVisible = false;
				}
				else {
					LoginJournal.IsVisible = false;
					Journal.IsVisible = true;
				}
			}
			//(base.SlideMenu as DabMenuView).ChangeAvatar();
			//base.SlideMenu = new DabMenuView();
		}

		void OnInitialized(object o, EventArgs e) {
			Initializer.IsVisible = false;
			if (AudioPlayer.Instance.IsInitialized)
			{
				AudioPlayer.Instance.Pause();
			}
			AudioPlayer.Instance.SetAudioFile(Episode);
			AudioPlayer.Instance.Play();
			SeekBar.IsVisible = true;
			TimeStrings.IsVisible = true;
			PlayerControls.IsVisible = true;
		}

		void OnShare(object o, EventArgs e) {
			Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(Episode.channel_code, Episode.id.ToString());
		}
	}
}
