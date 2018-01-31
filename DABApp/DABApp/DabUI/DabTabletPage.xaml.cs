using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        bool NotConstructing = false;

        public DabTabletPage(Resource resource, dbEpisodes Episode = null)
        {
            InitializeComponent();
            ReadText.EraseText = true;
            ArchiveHeader.Padding = Device.RuntimePlatform == "Android" ? new Thickness(20, 0, 20, 0) : new Thickness(10, 0, 10, 0);
            if (Device.RuntimePlatform == "Android" && Device.Idiom == TargetIdiom.Tablet)
            {
                if (GlobalResources.Instance.ScreenSize < 1000)
                {
                    PlayerOverlay.Padding = new Thickness(25, 10, 25, 25);
                    EpDescription.Margin = new Thickness(40, 0, 40, 0);
                }
            }
            SegControl.ValueChanged += Handle_ValueChanged;
            _resource = resource;
            ChannelsList.ItemsSource = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources;
            backgroundImage = _resource.images.backgroundTablet;
            BackgroundImage.Source = backgroundImage;
            Offline.IsToggled = _resource.availableOffline;
            Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                Months.Items.Add(month);
            }

            Months.Items.Add("My Journals");
            Months.Items.Add("My Favorites");
            Months.SelectedIndex = 0;
            TimedActions();
            MessagingCenter.Subscribe<string>("Update", "Update", (obj) => { TimedActions(); });
            if (Episode != null)
            {
                episode = Episode;
            }
            else
            {
                episode = Episodes.First();
            }
            favorite.BindingContext = episode;

            if (!GuestStatus.Current.IsGuestLogin)
            {
                JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
            }
            PlayerLabels.BindingContext = episode;
            Journal.BindingContext = episode;
            Task.Run(async () => { await SetReading(); });
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
            if (Device.RuntimePlatform == "iOS")
            {
                KeyboardHelper.KeyboardChanged += OnKeyboardChanged;
            }
            AudioPlayer.Instance.PlayerFailure += OnPlaybackStopped;
            var tapper = new TapGestureRecognizer();
            tapper.Tapped += (sender, e) =>
            {
                Device.OpenUri(new Uri("https://en.wikipedia.org/wiki/Markdown"));
            };
            AboutFormat.GestureRecognizers.Add(tapper);
            ChannelsList.SelectedItem = _resource;
            Completed.BindingContext = episode;
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

        public async void OnEpisode(object o, ItemTappedEventArgs e)
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            var newEp = (dbEpisodes)e.Item;
            JournalTracker.Current.Join(newEp.PubDate.ToString("yyyy-MM-dd"));
            if (newEp.is_downloaded || CrossConnectivity.Current.IsConnected)
            {
                episode = (dbEpisodes)e.Item;
                favorite.Source = episode.favoriteSource;
                if (AudioPlayer.Instance.CurrentEpisodeId != episode.id)
                {
                    JournalTracker.Current.Content = null;
                    SetVisibility(false);
                }
                else
                {
                    SetVisibility(true);
                }
                PlayerLabels.BindingContext = episode;
                JournalTitle.BindingContext = episode;
                EpisodeList.SelectedItem = null;
                await SetReading();
            }
            else await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played while offline download them before going offline.", "OK");
            Completed.Image = episode.listenedToSource;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
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
                Task.Run(async () => { await PlayerFeedAPI.DeleteChannelEpisodes(_resource); });
            }
        }

        public void OnMonthSelected(object o, EventArgs e)
        {
            if ((string)Months.SelectedItem == "My Favorites")
            {
                EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite == true);
            }
            else
            {
                if ((string)Months.SelectedItem == "My Journals")
                {
                    EpisodeList.ItemsSource = Episodes.Where(x => x.has_journal == true);
                }
                else
                {
                    if (Months.SelectedIndex >= 0)
                    {
                        EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
                    }
                }
            }
        }

        async void OnChannel(object o, EventArgs e)
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            if (CrossConnectivity.Current.IsConnected || PlayerFeedAPI.GetEpisodeList((Resource)ChannelsList.SelectedItem).Count() > 0)
            {
                _resource = (Resource)ChannelsList.SelectedItem;
                backgroundImage = _resource.images.backgroundTablet;
                if (NotConstructing)
                {
                    await PlayerFeedAPI.GetEpisodes(_resource);
                }
                NotConstructing = true;
                Offline.IsToggled = _resource.availableOffline;
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
                TimedActions();
                BackgroundImage.Source = backgroundImage;
                if (episode.PubDate != Episodes.First().PubDate)
                {
                    JournalTracker.Current.Join(Episodes.First().PubDate.ToString("yyyy-MM-dd"));
                }
                episode = Episodes.First();
                PlayerLabels.BindingContext = episode;
                JournalTitle.BindingContext = episode;
                await SetReading();
                if (AudioPlayer.Instance.CurrentEpisodeId != episode.id)
                {
                    SetVisibility(false);
                }
                else
                {
                    SetVisibility(true);
                }
                Completed.Image = episode.listenedToSource;
            }
            else await DisplayAlert("Unable to get episodes for channel.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
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
                    //Task.Run(async () =>
                    //{
                    //    await AuthenticationAPI.PostActionLogs();
                    //});
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
            if (Device.RuntimePlatform == "iOS")
            {
                JournalContent.HeightRequest = Content.Height * 2 / 3 - SegControl.Height - 90; //- Divider.Height
                original = Content.Height * 2 / 3 - SegControl.Height - -90; //- Divider.Height
            }
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
            AudioPlayer.Instance.Pause(); if ((string)Months.SelectedItem == "My Favorites")
            {
                EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite == true);
            }
            else
            {
                if ((string)Months.SelectedItem == "My Journals")
                {
                    EpisodeList.ItemsSource = Episodes.Where(x => x.has_journal == true);
                }
                else
                {
                    EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
                }
            }
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

        async Task SetReading()
        {
            Reading reading = await PlayerFeedAPI.GetReading(episode.read_link);
            //Update the Reading UI on the main thread after getting data from the API
            Device.BeginInvokeOnMainThread(() =>
            {
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
            });
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
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
        }

        void OnReconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Reconnected to journal server.", $"Journal changes will now be saved. {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Reconnected to journal server: {o.ToString()}");
        }

        void OnReconnecting(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Reconnecting to journal server.", $"On successful reconnection changes to journal will be saved. {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Reconnecting to journal server: {o.ToString()}");
        }

        void OnRoom_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("A room error has occured.", $"The journal server has sent back a room error. Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Room Error: {o.ToString()}");
        }

        void OnAuth_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("An auth error has occured.", $"The journal server has sent back an authentication error.  Try logging back in.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Auth Error: {o.ToString()}");
        }

        void OnJoin_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("A join error has occured.", $"The journal server has sent back a join error. Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Join error: {o.ToString()}");
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
                    JournalContent.HeightRequest = e.Visible ? original - e.Height : original;
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
            int opa = par == true ? 1 : 0;
            SeekBar.Opacity = opa;
            TimeStrings.Opacity = opa;
            backwardButton.Opacity = opa;
            forwardButton.Opacity = opa;
            //Output.IsVisible = par;
            //Share.IsVisible = par;
            //favorite.IsVisible = par;
            PlayPause.IsVisible = par;
            Initializer.IsVisible = !par;
        }

        async void OnFavorite(object o, EventArgs e)
        {
            favorite.IsEnabled = false;
            favorite.Opacity = .5;
            episode.is_favorite = !episode.is_favorite;
            favorite.Source = episode.favoriteSource;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.id, "is_favorite");
            await AuthenticationAPI.CreateNewActionLog((int)episode.id, "favorite", episode.stop_time, null, episode.is_favorite);
            favorite.Opacity = 1;
            favorite.IsEnabled = true;
            //EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
        }

        async void OnListListened(object o, EventArgs e)
        {
            var mi = ((MenuItem)o);
            var ep = (dbEpisodes)mi.CommandParameter;
            if (ep.is_listened_to == "listened")
            {
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, "");
                if (ep.id == episode.id)
                {
                    episode.is_listened_to = "";
                    Completed.Image = episode.listenedToSource;
                }
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "");
            }
            else
            {
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id);
                if (ep.id == episode.id)
                {
                    episode.is_listened_to = "listened";
                    Completed.Image = episode.listenedToSource;
                }
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "listened");
            }
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "");
            TimedActions();
        }

        async void OnListened(object o, EventArgs e)
        {
            if (episode.is_listened_to == "listened")
            {
                episode.is_listened_to = "";
                await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.id, "");
                await AuthenticationAPI.CreateNewActionLog((int)episode.id, "listened", episode.stop_time, "");
            }
            else
            {
                episode.is_listened_to = "listened";
                await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.id);
                await AuthenticationAPI.CreateNewActionLog((int)episode.id, "listened", episode.stop_time, "listened");
            }
            Completed.Image = episode.listenedToSource;
        }

        async void OnListFavorite(object o, EventArgs e)
        {
            var mi = ((MenuItem)o);
            var ep = (dbEpisodes)mi.CommandParameter;
            if (ep.id == episode.id)
            {
                OnFavorite(o, e);
            }
            else
            {
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, "is_favorite");
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "favorite", ep.stop_time, null, ep.is_favorite);
            }
            TimedActions();
        }

        async void OnRefresh(object o, EventArgs e)
        {
            //ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            //StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            //activity.IsVisible = true;
            //activityHolder.IsVisible = true;
            await PlayerFeedAPI.GetEpisodes(_resource);
            await AuthenticationAPI.GetMemberData();
            TimedActions();
            //activity.IsVisible = false;
            //activityHolder.IsVisible = false;
            EpisodeList.IsRefreshing = false;
        }

        void TimedActions()
        {
            if ((string)Months.SelectedItem == "My Favorites")
            {
                EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite == true);
            }
            else
            {
                if ((string)Months.SelectedItem == "My Journals")
                {
                    EpisodeList.ItemsSource = Episodes.Where(x => x.has_journal == true);
                }
                else
                {
                    if (Months.SelectedIndex >= 0)
                    {
                        EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
                    }
                }
            }
        }
    }
}
