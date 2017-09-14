using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SlideOverKit;
using Xamarin.Forms;
using TEditor;
using System.Threading.Tasks;
using Plugin.Connectivity;

namespace DABApp
{
	public partial class DabPlayerPage : DabBaseContentPage
	{
		//IAudio player = GlobalResources.Player;
		dbEpisodes Episode;
		string backgroundImage;
		bool IsGuest;
		static double original;

		public DabPlayerPage(dbEpisodes episode, Reading Reading)
		{
			InitializeComponent();
			SegControl.ValueChanged += Handle_ValueChanged;
			if (!GuestStatus.Current.IsGuestLogin)
			{
				JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
			}
			if (episode.id != AudioPlayer.Instance.CurrentEpisodeId) {
				SeekBar.IsVisible = false;
				TimeStrings.IsVisible = false;
				PlayerControls.VerticalOptions = LayoutOptions.CenterAndExpand;
				Output.IsVisible = false;
				PlayPause.IsVisible = false;
				backwardButton.IsVisible = false;
				forwardButton.IsVisible = false;
				Share.IsVisible = false;
				Initializer.IsVisible = true;
				Favorite.IsVisible = false;
			}
			IsGuest = GuestStatus.Current.IsGuestLogin;
			Episode = episode;
			//AudioPlayer.Instance.ShowPlayerBar = false;
			SeekBar.Value = AudioPlayer.Instance.CurrentTime;
			SeekBar.UserInteraction += OnTouch;
			DabViewHelper.InitDabForm(this);
			backgroundImage = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == episode.channel_title).images.backgroundPhone;
			BackgroundImage.Source = backgroundImage;
			BindingContext = episode;
			//Date.Text = $"{episode.PubMonth} {episode.PubDay.ToString()} {episode.PubYear.ToString()}";
			base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			Reading reading = Reading;

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
			JournalTracker.Current.socket.Disconnect += OnDisconnect;
			JournalTracker.Current.socket.Reconnect += OnReconnect;
			JournalTracker.Current.socket.Reconnecting += OnReconnecting;
			JournalTracker.Current.socket.Room_Error += OnRoom_Error;
			JournalTracker.Current.socket.Auth_Error += OnAuth_Error;
			JournalTracker.Current.socket.Join_Error += OnJoin_Error;
			if (Device.RuntimePlatform == "iOS")
			{
				KeyboardHelper.KeyboardChanged += OnKeyboardChanged;
			}
			AudioPlayer.Instance.PlayerFailure += OnPlayerFailure;
			var tapper = new TapGestureRecognizer();
			tapper.Tapped += (sender, e) => 
			{
				Device.OpenUri(new Uri("https://en.wikipedia.org/wiki/Markdown"));
			};
			AboutFormat.GestureRecognizers.Add(tapper);
			Favorite.BindingContext = Episode;
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
					Divider.IsVisible = false;
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
					Divider.IsVisible = true;
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
					Divider.IsVisible = true;
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

		void OnTouch(object o, EventArgs e) {
			AudioPlayer.Instance.IsTouched = true;
		}

		void OnPodcast(object o, EventArgs e)
		{
			SegControl.SelectedSegment = 0;
		}

		void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			if (CrossConnectivity.Current.IsConnected)
			{
				var nav = new NavigationPage(new DabLoginPage(true));
				nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
				Navigation.PushModalAsync(nav);
			}
			else DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
			Login.IsEnabled = true;
		}

		void OnJournalChanged(object o, EventArgs e)
		{
			if (JournalContent.IsFocused)
			{
				JournalTracker.Current.Update(Episode.PubDate.ToString("yyyy-MM-dd"), JournalContent.Text);
			}
		}

		void OnEdit(object o, EventArgs e)
		{
			JournalTracker.Current.socket.ExternalUpdate = false;
		}

		void OffEdit(object o, EventArgs e)
		{
			JournalTracker.Current.socket.ExternalUpdate = true;
			if (!JournalTracker.Current.IsJoined)
			{
				JournalTracker.Current.Join(Episode.PubDate.ToString("yyyy-MM-dd"));
			}
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (Device.RuntimePlatform == "iOS")
			{
				int paddingMulti = JournalTracker.Current.IsConnected ? 4 : 6;
				JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
				original = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
			}

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
			if (!GuestStatus.Current.IsGuestLogin && !JournalTracker.Current.IsJoined)
			{
				JournalTracker.Current.Join(Episode.PubDate.ToString("yyyy-MM-dd"));
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
			PlayerControls.VerticalOptions = LayoutOptions.StartAndExpand;
			Output.IsVisible = true;
			PlayPause.IsVisible = true;
			backwardButton.IsVisible = true;
			forwardButton.IsVisible = true;
			Share.IsVisible = true;
			Favorite.IsVisible = true;
		}

		void OnShare(object o, EventArgs e) {
			Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(Episode.channel_code, Episode.id.ToString());
		}

		void OnDisconnect(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
			});
			JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom* 6;
			original = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * 6;
		}

		void OnReconnect(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Reconnected to journal server.", $"Journal changes will now be saved. {o.ToString()}", "OK");
			});
			JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * 4;
			original = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * 4;
		}

		void OnReconnecting(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Reconnecting to journal server.", $"On successful reconnection changes to journal will be saved. {o.ToString()}", "OK");
			});
		}

		void OnRoom_Error(object o, EventArgs e) 
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("A room error has occured.", $"The journal server has sent back a room error. Error: {o.ToString()}", "OK");
			});
		}

		void OnAuth_Error(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("An auth error has occured.", $"The journal server has sent back an authentication error.  Try logging back in.  Error: {o.ToString()}", "OK");
			});
		}

		void OnJoin_Error(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("A join error has occured.", $"The journal server has sent back a join error. Error: {o.ToString()}", "OK");
			});
		}

		void OnKeyboardChanged(object o, KeyboardHelperEventArgs e)
		{
			if (JournalTracker.Current.Open)
			{
				spacer.HeightRequest = e.Visible ? e.Height : 0;
				if (e.IsExternalKeyboard)
				{
					JournalContent.HeightRequest = original;
				}
				else
				{
					JournalContent.HeightRequest = e.Visible ? JournalContent.Height - e.Height : original;
				}
			}
		}

		async void OnPlayerFailure(object o, EventArgs e)
		{
			await DisplayAlert("Audio Playback has stopped.", "If you are currently streaming this may be due to a loss of or poor internet connectivity.  Please check your connection and try again.", "OK");
		}

		void OnFavorite(object o, EventArgs e)
		{
			Episode.is_favorite = !Episode.is_favorite;
			Favorite.Image = Episode.favoriteSource;
			PlayerFeedAPI.UpdateEpisodeProperty(Episode.id, "is_favorite");
			AuthenticationAPI.CreateNewActionLog(Episode.id, "favorite", Episode.stop_time, Episode.is_favorite);
		}
	}
}
