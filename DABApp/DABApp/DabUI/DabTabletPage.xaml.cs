using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTabletPage : DabBaseContentPage
	{
		Resource _resource;
		IEnumerable<dbEpisodes> Episodes;
		string backgroundImage;
		dbEpisodes episode;
		static double original;
		static bool lastKeyboardStatus;

		public DabTabletPage(Resource resource, dbEpisodes Episode = null)
		{
			InitializeComponent();
			_resource = resource;
			ChannelsList.ItemsSource = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources;
			ChannelsList.SelectedItem = _resource;
			backgroundImage = _resource.images.backgroundTablet;
			BackgroundImage.Source = backgroundImage;
			Offline.IsToggled = _resource.availableOffline;
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
			foreach (var month in months)
			{
				Months.Items.Add(month);
			}
			Months.Items.Add("My Favorites");
			Months.SelectedIndex = 0;
			Device.StartTimer(TimeSpan.FromSeconds(5), () =>
			{
				EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
				return true;
			});
			if (Episode != null)
			{
				episode = Episode;
			}
			else
			{
				episode = Episodes.First();
			}
			if (!GuestStatus.Current.IsGuestLogin)
			{
				JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
			}
			PlayerLabels.BindingContext = episode;
			Journal.BindingContext = episode;
			SetReading();
			if (episode == null)
			{
				SetVisibility(false);
			}
			else if (episode.id != AudioPlayer.Instance.CurrentEpisodeId)
			{
				SetVisibility(false);
			}
			JournalTracker.Current.socket.Disconnect += OnDisconnect;
			JournalTracker.Current.socket.Reconnect += OnReconnect;
			JournalTracker.Current.socket.Reconnecting += OnReconnecting;
			JournalTracker.Current.socket.Room_Error += OnRoom_Error;
			JournalTracker.Current.socket.Auth_Error += OnAuth_Error;
			JournalTracker.Current.socket.Join_Error += OnJoin_Error;
			KeyboardHelper.KeyboardChanged += OnKeyboardChanged;
			AudioPlayer.Instance.PlayerFailure += OnPlaybackStopped;
			var tapper = new TapGestureRecognizer();
			tapper.Tapped += (sender, e) => {
				Device.OpenUri(new Uri("https://en.wikipedia.org/wiki/Markdown"));
			};
			AboutFormat.GestureRecognizers.Add(tapper);
		}

		void Handle_ValueChanged(object sender, System.EventArgs e)
		{
			switch (SegControl.SelectedSegment)
			{
				case 0:
					if (GuestStatus.Current.IsGuestLogin)
					{
						LoginJournal.IsVisible = false;
					}
					else { Journal.IsVisible = false; }
					Read.IsVisible = false;
					Journal.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = false;
					Archive.IsVisible = true;
					break;
				case 1:
					Archive.IsVisible = false;
					if (GuestStatus.Current.IsGuestLogin)
					{
						LoginJournal.IsVisible = false;
					}
					else
					{
						Journal.IsVisible = false;
					}
					//AudioPlayer.Instance.showPlayerBar = true;
					Read.IsVisible = true;
					break;
				case 2:
					Read.IsVisible = false;
					Archive.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = true;
					if (GuestStatus.Current.IsGuestLogin)
					{
						LoginJournal.IsVisible = true;
					}
					else
					{
						Journal.IsVisible = true;
					}
					break;
			}
		}

		public void OnEpisode(object o, ItemTappedEventArgs e)
		{
			episode = (dbEpisodes)e.Item;
			if (AudioPlayer.Instance.CurrentEpisodeId != episode.id)
			{
				SetVisibility(false);
			}
			else
			{
				SetVisibility(true);
			}
			PlayerLabels.BindingContext = episode;
			EpisodeList.SelectedItem = null;
			SetReading();
		}

		public void OnOffline(object o, ToggledEventArgs e)
		{
			_resource.availableOffline = e.Value;
			ContentAPI.UpdateOffline(e.Value, _resource.id);
			if (e.Value)
			{
				Task.Run(async () => { 
					await PlayerFeedAPI.DownloadEpisodes(); 
				});
			}
			else
			{
				PlayerFeedAPI.DeleteChannelEpisodes(_resource);
			}
		}

		public void OnMonthSelected(object o, EventArgs e)
		{
			if ((string)Months.SelectedItem != "My Favorites")
			{
				EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
			}
			else 
			{
				EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite == true);
			}
		}

		async void OnChannel(object o, EventArgs e)
		{
			_resource = (Resource)ChannelsList.SelectedItem;
			backgroundImage = _resource.images.backgroundTablet;
			await PlayerFeedAPI.GetEpisodes(_resource);
			Offline.IsToggled = _resource.availableOffline;
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			EpisodeList.ItemsSource = Episodes;
			BackgroundImage.Source = backgroundImage;
			episode = Episodes.First();
			PlayerLabels.BindingContext = episode;
			SetReading();
			if (AudioPlayer.Instance.CurrentEpisodeId != episode.id)
			{
				SetVisibility(false);
			}
			else
			{
				SetVisibility(true);
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

		void OnPlay(object o, EventArgs e)
		{
			if (AudioPlayer.Instance.IsInitialized)
			{
				if (AudioPlayer.Instance.IsPlaying)
				{
					AudioPlayer.Instance.Pause();
				}
				else
				{
					AudioPlayer.Instance.Play();
				}
			}
			else
			{
				AudioPlayer.Instance.SetAudioFile(episode);
				AudioPlayer.Instance.Play();
			}
		}

		void OnShare(object o, EventArgs e)
		{
			Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(episode.channel_code, episode.id.ToString());
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			JournalContent.HeightRequest = Content.Height*2/3 - SegControl.Height - Divider.Height - 90;
			original = Content.Height*2/3 - SegControl.Height - Divider.Height - 90;
			if (LoginJournal.IsVisible || Journal.IsVisible)
			{
				if (GuestStatus.Current.IsGuestLogin)
				{
					LoginJournal.IsVisible = true;
					Journal.IsVisible = false;
				}
				else
				{
					LoginJournal.IsVisible = false;
					Journal.IsVisible = true;
				}
			}
			if (episode != null && !GuestStatus.Current.IsGuestLogin)
			{
				JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
			}
		}

		void OnLogin(object o, EventArgs e)
		{
			Login.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			if (CrossConnectivity.Current.IsConnected)
			{
				var nav = new NavigationPage(new DabLoginPage(true));
				nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
				Navigation.PushModalAsync(nav);
			}
            else DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
			Login.IsEnabled = true;
		}

		async void SetReading()
		{
			Reading reading = await PlayerFeedAPI.GetReading(episode.read_link);
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
			else ReadExcerpts.Text = "";
		}

		void OnInitialized(object o, EventArgs e)
		{
			Initializer.IsVisible = false;
			if (AudioPlayer.Instance.IsInitialized)
			{
				AudioPlayer.Instance.Pause();
			}
			AudioPlayer.Instance.SetAudioFile(episode);
			AudioPlayer.Instance.Play();
			SetVisibility(true);
		}

		void OnJournalChanged(object o, EventArgs e)
		{
			if (JournalContent.IsFocused)
			{
				JournalTracker.Current.Update(episode.PubDate.ToString("yyyy-MM-dd"), JournalContent.Text);
			}
		}

		void OnTouched(object o, EventArgs e)
		{
			AudioPlayer.Instance.IsTouched = true;
		}

		void OnDisconnect(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
			});
		}

		void OnReconnect(object o, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Reconnected to journal server.", $"Journal changes will now be saved. {o.ToString()}", "OK");
			});
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

		void OnEdit(object o, EventArgs e)
		{
			JournalTracker.Current.socket.ExternalUpdate = false;
		}

		void OffEdit(object o, EventArgs e)
		{
			JournalTracker.Current.socket.ExternalUpdate = true;
			if (!JournalTracker.Current.socket.IsJoined)
			{
				JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
			}
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
					JournalContent.HeightRequest = e.Visible ? JournalContent.Height - e.Height : JournalContent.Height + e.Height;
				}
				//lastKeyboardStatus = e.IsExternalKeyboard;
			}
		}

		async void OnPlaybackStopped(object o, EventArgs e)
		{
			await DisplayAlert("Audio Playback has stopped.", "If you are currently streaming this may be due to a loss of or poor internet connectivity.  Please check your connection and try again.", "OK");
		}

		void SetVisibility(bool par)
		{ 
			SeekBar.IsVisible = par;
			TimeStrings.IsVisible = par;
			Output.IsVisible = par;
			PlayPause.IsVisible = par;
			backwardButton.IsVisible = par;
			forwardButton.IsVisible = par;
			Share.IsVisible = par;
			favorite.IsVisible = par;
			Initializer.IsVisible = !par;
		}
	}
}
