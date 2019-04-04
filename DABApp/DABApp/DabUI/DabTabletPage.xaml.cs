using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Acr.DeviceInfo;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabTabletPage : DabBaseContentPage
    {
        Resource _resource;
        IEnumerable<dbEpisodes> Episodes;
        ObservableCollection<EpisodeViewModel> list;
        string backgroundImage;
        EpisodeViewModel episode;
        double original;
        bool NotConstructing = false;
        private double _width;
        private double _height;

        public DabTabletPage(Resource resource, dbEpisodes Episode = null)
        {
            InitializeComponent();
            _width = this.Width;
            _height = this.Height;
            ReadText.EraseText = true;
            ArchiveHeader.Padding = Device.RuntimePlatform == "Android" ? new Thickness(20, 0, 20, 0) : new Thickness(10, 0, 10, 0);
            PlayerOverlay.Padding = new Thickness(25, 10, 25, 25);
            EpDescription.Margin = new Thickness(40, 0, 40, 0);
            JournalContent.HeightRequest = 450;
            SegControl.ValueChanged += Handle_ValueChanged;
            _resource = resource;
            ChannelsList.ItemsSource = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources;
            backgroundImage = _resource.images.backgroundTablet;
            BackgroundImage.Source = backgroundImage;
            Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                var m = MonthConverter.ConvertToFull(month);
                Months.Items.Add(m);
            }
            Months.Items.Insert(0, "All Episodes");
            Months.SelectedIndex = 0;
            TimedActions();
            MessagingCenter.Subscribe<string>("Update", "Update", (obj) =>
            {
                TimedActions();
            });
            if (Episode != null)
            {
                episode = new EpisodeViewModel(Episode);
            }
            else
            {
                episode = new EpisodeViewModel(Episodes.First());
            }
            favorite.BindingContext = episode;

            if (!GuestStatus.Current.IsGuestLogin)
            {
                JournalTracker.Current.Join(episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }
            PlayerLabels.BindingContext = episode;
            Journal.BindingContext = episode;
            Task.Run(async () => { await SetReading(); });
            if (episode == null)
            {
                SetVisibility(false);
            }
            else if (episode.Episode.id != AudioPlayer.Instance.CurrentEpisodeId)
            {
                SetVisibility(false);
            }
            JournalTracker.Current.socket.Disconnect += OnDisconnect;
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
            if (Device.RuntimePlatform == "Android" && Device.Idiom == TargetIdiom.Tablet)
            {
                SegControl.IsVisible = false;
                SegControl.IsVisible = true;
            }
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

                    //Send info to Firebase analytics that user tapped the read tab
                    var info = new Dictionary<string, string>();
                    info.Add("channel", episode.Episode.channel_title);
                    info.Add("episode_date", episode.Episode.PubDate.ToString());
                    info.Add("episode_name", episode.title);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_read", info);
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
                    //Send info to Firebase analytics that user tapped the journal tab
                    var infoJ = new Dictionary<string, string>();
                    infoJ.Add("channel", episode.Episode.channel_title);
                    infoJ.Add("episode_date", episode.Episode.PubDate.ToString());
                    infoJ.Add("episode_name", episode.Episode.title);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_journal", infoJ);
                    break;
            }
        }

        public async void OnEpisode(object o, ItemTappedEventArgs e)
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout labelHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "labelHolder");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            labelHolder.IsVisible = true;
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            var newEp = (EpisodeViewModel)e.Item;
            JournalTracker.Current.Join(newEp.Episode.PubDate.ToString("yyyy-MM-dd"));
            var ext = newEp.Episode.url.Split('.').Last();
            if (DependencyService.Get<IFileManagement>().FileExists($"{newEp.Episode.id.ToString()}.{ext}") || CrossConnectivity.Current.IsConnected)
            {
                episode = (EpisodeViewModel)e.Item;
                favorite.Source = episode.favoriteSource;
                if (AudioPlayer.Instance.CurrentEpisodeId != episode.Episode.id)
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

                //Send info to Firebase analytics that user accessed and episode
                var infoJ = new Dictionary<string, string>();
                infoJ.Add("channel", episode.Episode.channel_title);
                infoJ.Add("episode_date", episode.Episode.PubDate.ToString());
                infoJ.Add("episode_name", episode.Episode.title);
                DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_selected", infoJ);
            }
            else await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played while offline download them before going offline.", "OK");
            Completed.Image = episode.listenedToSource;
            labelHolder.IsVisible = false;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
        }

        public void OnMonthSelected(object o, EventArgs e)
        {
            TimedActions();
        }

        async void OnChannel(object o, EventArgs e)
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            StackLayout labelHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "labelHolder");
            labelHolder.IsVisible = true;
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
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
                TimedActions();
                BackgroundImage.Source = backgroundImage;
                if (episode.Episode.PubDate != Episodes.First().PubDate)
                {
                    JournalTracker.Current.Join(Episodes.First().PubDate.ToString("yyyy-MM-dd"));
                }
                episode = new EpisodeViewModel(Episodes.First());
                PlayerLabels.BindingContext = episode;
                JournalTitle.BindingContext = episode;
                await SetReading();
                if (AudioPlayer.Instance.CurrentEpisodeId != episode.Episode.id)
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
            labelHolder.IsVisible = false;
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
                AudioPlayer.Instance.SetAudioFile(episode.Episode);
                AudioPlayer.Instance.Play();
            }
        }

        void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(episode.Episode.channel_code, episode.Episode.id.ToString());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Device.RuntimePlatform == "iOS")
            {
                JournalContent.HeightRequest = Content.Height * 2 / 3 - SegControl.Height - 90; //- Divider.Height
                //original = Content.Height * 2 / 3 - SegControl.Height - -90; //- Divider.Height
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
                JournalTracker.Current.Join(episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }
            if (Initializer.IsVisible)
            {
                Initializer.Focus();
            }
            else
            {
                PlayPause.Focus();
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
            Reading reading = await PlayerFeedAPI.GetReading(episode.Episode.read_link);
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
            //if (AudioPlayer.Instance.IsInitialized)
            //{
            //    AudioPlayer.Instance.Pause();
            //}
            AudioPlayer.Instance.SetAudioFile(episode.Episode);
            AudioPlayer.Instance.Play();
            SetVisibility(true);
        }

        void OnJournalChanged(object o, EventArgs e)
        {
            if (JournalContent.IsFocused)
            {
                JournalTracker.Current.Update(episode.Episode.PubDate.ToString("yyyy-MM-dd"), JournalContent.Text);
            }
        }

        void OnDisconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
        }

        async void OnReconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Reconnected to journal server.", $"Journal changes will now be saved. {o.ToString()}", "OK");
            //});
            JournalWarning.IsEnabled = false;
            AuthenticationAPI.ConnectJournal();
            Debug.WriteLine($"Reconnected to journal server: {o.ToString()}");
            await Task.Delay(1000);
            if (!JournalTracker.Current.IsConnected)
            {
                await DisplayAlert("Unable to reconnect to journal server", "Please check your internet connection and try again.", "OK");
            }
            JournalWarning.IsEnabled = true;
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
                JournalTracker.Current.Join(episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }
        }

        void OnKeyboardChanged(object o, KeyboardHelperEventArgs e)
        {
            if (JournalTracker.Current.Open && original != 0)
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
            episode.Episode.is_favorite = !episode.Episode.is_favorite;
            favorite.Source = episode.favoriteSource;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, "is_favorite");
            await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "favorite", episode.Episode.stop_time, null, episode.Episode.is_favorite);
            favorite.Opacity = 1;
            favorite.IsEnabled = true;
            AutomationProperties.SetName(favorite, episode.favoriteAccessible);
            //EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
            TimedActions();
        }

        async void OnListListened(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            if (ep.is_listened_to == "listened")
            {
                model.listenedToVisible = false;
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, "");
                if (ep.id == episode.Episode.id)
                {
                    episode.Episode.is_listened_to = "";
                    Completed.Image = episode.listenedToSource;
                    AutomationProperties.SetHelpText(Completed, episode.listenAccessible);
                }
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "");
            }
            else
            {
                model.listenedToVisible = true;
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id);
                if (ep.id == episode.Episode.id)
                {
                    episode.Episode.is_listened_to = "listened";
                    Completed.Image = episode.listenedToSource;
                    AutomationProperties.SetHelpText(Completed, episode.listenAccessible);
                }
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "listened");
            }
        }

        async void OnListened(object o, EventArgs e)
        {
            if (episode.Episode.is_listened_to == "listened")
            {
                episode.Episode.is_listened_to = "";
                await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, "");
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", episode.Episode.stop_time, "");
            }
            else
            {
                episode.Episode.is_listened_to = "listened";
                await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id);
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", episode.Episode.stop_time, "listened");
            }
            Completed.Image = episode.listenedToSource;
            AutomationProperties.SetName(Completed, episode.listenAccessible);
            TimedActions();
        }

        async void OnListFavorite(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            if (ep.id == episode.Episode.id)
            {
                episode.favoriteVisible = !ep.is_favorite;
                favorite.Source = episode.favoriteSource;
            }
            model.favoriteVisible = !ep.is_favorite;
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, "is_favorite");
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "favorite", ep.stop_time, null, !ep.is_favorite);
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            double oldwidth = _width;
            base.OnSizeAllocated(width, height);
            if (Equals(_width, width) && Equals(_height, height)) return;
            _width = width;
            _height = height;
            if (Equals(oldwidth, -1)) return;
            if (width > height)
            {
                BesidesPlayer.Height = new GridLength(1, GridUnitType.Star);
                CenterVoid.Width = new GridLength(8, GridUnitType.Star);
                LeftVoid.Width = new GridLength(38, GridUnitType.Star);
                RightVoid.Width = new GridLength(38, GridUnitType.Star);
                SegControlContainer.Padding = new Thickness(200, 20, 200, 0);
                BackgroundImage.Aspect = Aspect.Fill;
                var size = 60;
                PlayPause.WidthRequest = size;
                PlayPause.HeightRequest = size;
                Initializer.WidthRequest = size;
                Initializer.HeightRequest = size;
                backwardButton.Margin = 7;
                forwardButton.Margin = 7;
                JournalContent.HeightRequest = Device.RuntimePlatform == Device.iOS ? height * .3 : height * .3;
                original = 0;
            }
            else
            {
                BesidesPlayer.Height = new GridLength(2, GridUnitType.Star);
                SegControlContainer.Padding = new Thickness(20, 20, 20, 0);
                CenterVoid.Width = new GridLength(4, GridUnitType.Star);
                LeftVoid.Width = new GridLength(24, GridUnitType.Star);
                RightVoid.Width = new GridLength(24, GridUnitType.Star);
                BackgroundImage.Aspect = Aspect.AspectFill;
                backwardButton.Margin = 5;
                forwardButton.Margin = 5;
                if (GlobalResources.Instance.ScreenSize < 1000)
                {
                    PlayPause.WidthRequest = 90;
                    PlayPause.HeightRequest = 90;
                    Initializer.WidthRequest = 90;
                    Initializer.HeightRequest = 90;
                }
                JournalContent.HeightRequest = Device.RuntimePlatform == Device.iOS ? height * .5 : height * .5;
                original = JournalContent.HeightRequest;
            }
        }

        async void OnRefresh(object o, EventArgs e)
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            StackLayout labelHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "labelHolder");
            labelHolder.IsVisible = true;
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            await AuthenticationAPI.PostActionLogs();
            await PlayerFeedAPI.GetEpisodes(_resource);
            await AuthenticationAPI.GetMemberData();
            episode = new EpisodeViewModel(PlayerFeedAPI.GetEpisode(episode.Episode.id.Value));
            TimedActions();
            labelHolder.IsVisible = false;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
            EpisodeList.IsRefreshing = false;
        }

        async void OnFilters(object o, EventArgs e)
        {
            var popup = new DabPopupEpisodeMenu(_resource);
            popup.ChangedRequested += Popup_ChangedRequested;
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(popup, false);
        }

        private void Popup_ChangedRequested(object sender, EventArgs e)
        {
            var popuPage = (DabPopupEpisodeMenu)sender;
            _resource = popuPage.Resource;
            TimedActions();
        }

        void TimedActions()
        {
            if (_resource.AscendingSort)
            {
                Episodes = Episodes.OrderBy(x => x.PubDate);
            }
            else
            {
                Episodes = Episodes.OrderByDescending(x => x.PubDate);
            }
            EpisodeList.ItemsSource = list = new ObservableCollection<EpisodeViewModel>(Episodes
                .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Months.Items[Months.SelectedIndex].Substring(0, 3))
                .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.is_favorite : true)
                .Where(x => _resource.filter == EpisodeFilters.Journal ? x.has_journal : true)
                .Select(x => new EpisodeViewModel(x)));
            if (episode != null)
            {
                favorite.Source = episode.favoriteSource;
                Completed.Image = episode.listenedToSource;
            }
        }
    }
}
