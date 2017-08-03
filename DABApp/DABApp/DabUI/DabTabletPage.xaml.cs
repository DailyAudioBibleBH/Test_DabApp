using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTabletPage : DabBaseContentPage
	{
		Resource _resource;
		IEnumerable<dbEpisodes> Episodes;
		string backgroundImage;
		dbEpisodes episode;

		public DabTabletPage(Resource resource, dbEpisodes Episode = null)
		{
			InitializeComponent();
			_resource = resource;
			ChannelsList.ItemsSource = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources;
			ChannelsList.SelectedItem = _resource;
			backgroundImage = _resource.images.backgroundTablet;
			BackgroundImage.Source = backgroundImage;
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
			foreach (var month in months)
			{
				Months.Items.Add(month);
			}
			Months.SelectedIndex = 0;
			//Device.StartTimer( TimeSpan.FromSeconds(5),() =>
			//{
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
			if (Episode != null)
			{
				episode = Episode;
			}
			else
			{
				episode = Episodes.First();
			}
			JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
			PlayerLabels.BindingContext = episode;
			Journal.BindingContext = episode;
			SetReading();
			if (episode == null)
			{
				SeekBar.IsVisible = false;
				TimeStrings.IsVisible = false;
				Output.IsVisible = false;
				PlayPause.IsVisible = false;
				backwardButton.IsVisible = false;
				forwardButton.IsVisible = false;
				Share.IsVisible = false;
				Initializer.IsVisible = true;
			}
			else if (episode.id != AudioPlayer.Instance.CurrentEpisodeId)
			{
				SeekBar.IsVisible = false;
				TimeStrings.IsVisible = false;
				Output.IsVisible = false;
				PlayPause.IsVisible = false;
				backwardButton.IsVisible = false;
				forwardButton.IsVisible = false;
				Share.IsVisible = false;
				Initializer.IsVisible = true;
			}
			JournalTracker.Current.socket.Disconnect += OnDisconnect;
			JournalTracker.Current.socket.Reconnect += OnReconnect;
			JournalTracker.Current.socket.Reconnecting += OnReconnecting;
			JournalTracker.Current.socket.Room_Error += OnRoom_Error;
			JournalTracker.Current.socket.Auth_Error += OnAuth_Error;
			JournalTracker.Current.socket.Join_Error += OnJoin_Error;

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
				SeekBar.IsVisible = false;
				TimeStrings.IsVisible = false;
				Output.IsVisible = false;
				PlayPause.IsVisible = false;
				backwardButton.IsVisible = false;
				forwardButton.IsVisible = false;
				Share.IsVisible = false;
				Initializer.IsVisible = true;
			}
			else
			{
				SeekBar.IsVisible = true;
				TimeStrings.IsVisible = true;
				Output.IsVisible = true;
				PlayPause.IsVisible = true;
				backwardButton.IsVisible = true;
				forwardButton.IsVisible = true;
				Share.IsVisible = true;
				Initializer.IsVisible = false;
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
				Task.Run(async () => { await PlayerFeedAPI.DownloadEpisodes(); });
			}
			else
			{
				PlayerFeedAPI.DeleteChannelEpisodes(_resource);
			}
		}

		public void OnMonthSelected(object o, EventArgs e)
		{
			EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
		}

		void OnChannel(object o, EventArgs e)
		{
			_resource = (Resource)ChannelsList.SelectedItem;
			backgroundImage = _resource.images.backgroundTablet;
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			EpisodeList.ItemsSource = Episodes;
			BackgroundImage.Source = backgroundImage;
			episode = Episodes.First();
			PlayerLabels.BindingContext = episode;
			SetReading();
			if (AudioPlayer.Instance.CurrentEpisodeId != episode.id)
			{
				SeekBar.IsVisible = false;
				TimeStrings.IsVisible = false;
				Output.IsVisible = false;
				PlayPause.IsVisible = false;
				backwardButton.IsVisible = false;
				forwardButton.IsVisible = false;
				Share.IsVisible = false;
				Initializer.IsVisible = true;
			}
			else
			{
				SeekBar.IsVisible = true;
				TimeStrings.IsVisible = true;
				Output.IsVisible = true;
				PlayPause.IsVisible = true;
				backwardButton.IsVisible = true;
				forwardButton.IsVisible = true;
				Share.IsVisible = true;
				Initializer.IsVisible = false;
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
		}

		void OnLogin(object o, EventArgs e)
		{
			Login.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			var nav = new NavigationPage(new DabLoginPage(true));
			nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
			Navigation.PushModalAsync(nav);
			Login.IsEnabled = true;
		}

		void SetReading()
		{
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
			SeekBar.IsVisible = true;
			TimeStrings.IsVisible = true;
			Output.IsVisible = true;
			PlayPause.IsVisible = true;
			backwardButton.IsVisible = true;
			forwardButton.IsVisible = true;
			Share.IsVisible = true;
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
			JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
		}
	}
}
