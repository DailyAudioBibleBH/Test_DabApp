using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Acr.DeviceInfo;
using DABApp.DabAudio;
using DABApp.DabSockets;
using Plugin.Connectivity;
using Xamarin.Forms;


/* This page is the tablet player page and controls channels, episodes, and player on tablet devices
 * */

namespace DABApp
{
    public partial class DabTabletPage : DabBaseContentPage
    {
        DabPlayer player = GlobalResources.playerPodcast; //Reference to the global podcast player object
        Resource _resource; //The current resource (initially passed from the channels page)
        IEnumerable<dbEpisodes> Episodes; //List of episodes for current channel
        ObservableCollection<EpisodeViewModel> list;
        string backgroundImage; //background image in the header?
        EpisodeViewModel episode;
        double original;
        private double _width; //screen width
        private double _height; //screen height
        DabJournalService journal;


        //Open up the page and optionally init it with an episode
        public DabTabletPage(Resource resource, dbEpisodes Episode = null)
        {
            InitializeComponent();

            //Prepare an empty journal object (needed early for binding purposes)
            journal = new DabJournalService();

            //UI Setup
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];

            _width = this.Width; //store current width and height
            _height = this.Height;

            //custom padding and sizing needs
            ArchiveHeader.Padding = Device.RuntimePlatform == "Android" ? new Thickness(20, 0, 20, 0) : new Thickness(10, 0, 10, 0);
            PlayerOverlay.Padding = new Thickness(25, 10, 25, 25);
            EpDescription.Margin = new Thickness(40, 0, 40, 0);
            JournalContent.HeightRequest = 450;

            //Set up events
            SegControl.ValueChanged += Handle_SegControlValueChanged;

            //Channels list
            ChannelsList.ItemsSource = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources;
            ChannelsList.SelectedItem = _resource;

            //Set up selected channel
            _resource = resource;
            backgroundImage = _resource.images.backgroundTablet;
            BackgroundImage.Source = backgroundImage;
            Episodes = PlayerFeedAPI.GetEpisodeList(_resource); //Get episodes for selected channel

            //Break episode months out into a list
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                var m = MonthConverter.ConvertToFull(month);
                Months.Items.Add(m);
            }
            Months.Items.Insert(0, "All Episodes");
            Months.SelectedIndex = 0;

            //Run timed actions and subscribe to events to update them
            TimedActions();
            MessagingCenter.Subscribe<string>("Update", "Update", (obj) =>
            {
                TimedActions();
            });

            //Load the specified episode
            if (Episode != null)
            {
                episode = new EpisodeViewModel(Episode);
            }
            else
            {
                //Pick the first episode
                episode = new EpisodeViewModel(Episodes.First());
            }
            //Bind to the active episode
            favorite.BindingContext = episode;
            PlayerLabels.BindingContext = episode;
            Completed.BindingContext = episode;
            BindControls(true, true);


            //Reading area
            ReadText.EraseText = true;
            Task.Run(async () => { await SetReading(); });
            if (episode == null)
            {
                SetVisibility(false);
            }
            else if (episode.Episode.id != GlobalResources.CurrentEpisodeId)
            {
                SetVisibility(false);
            }


            //Journal area
            //TODO: Replace for journal?
            if (!GuestStatus.Current.IsGuestLogin)
            {
                //Join the journal channel
                journal.InitAndConnect();
                journal.JoinRoom(episode.Episode.PubDate);
            }
            //Journal.BindingContext = episode;
            //JournalTracker.Current.socket.Disconnect += OnDisconnect;
            //JournalTracker.Current.socket.Reconnecting += OnReconnecting;
            //JournalTracker.Current.socket.Room_Error += OnRoom_Error;
            //JournalTracker.Current.socket.Auth_Error += OnAuth_Error;
            //JournalTracker.Current.socket.Join_Error += OnJoin_Error;

            //Keyboard events on iOS for Journal
            if (Device.RuntimePlatform == "iOS")
            {
                KeyboardHelper.KeyboardChanged += OnKeyboardChanged;
            }

            //Tap event for the MarkDownDeep link
            var tapper = new TapGestureRecognizer();
            tapper.Tapped += (sender, e) =>
            {
                Device.OpenUri(new Uri("https://en.wikipedia.org/wiki/Markdown"));
            };
            AboutFormat.GestureRecognizers.Add(tapper);

            MessagingCenter.Subscribe<string>("dabapp", "EpisodeDataChanged", (obj) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    //Get fresh reference to the episode
                    episode = new EpisodeViewModel(PlayerFeedAPI.GetEpisode(episode.Episode.id.Value));
                    BindControls(true, true);
                    //Bind episode data in the list
                    TimedActions();
                });

            }); 
        }

        void Handle_SegControlValueChanged(object sender, System.EventArgs e)
        /* Select different segment (play/read/journal) */
        {
            if (Device.RuntimePlatform == "Android" && Device.Idiom == TargetIdiom.Tablet)
            {
                //Android tablet specific code for segment control - hide and show again?
                SegControl.IsVisible = false;
                SegControl.IsVisible = true;
            }
            switch (SegControl.SelectedSegment)
            {
                case 0: //READ
                    //Show Episode List Only
                    Archive.IsVisible = true;
                    //Hide Read
                    Read.IsVisible = false;
                    //Hide Journal
                    LoginJournal.IsVisible = false;
                    Journal.IsVisible = false;
                    break;
                case 1: //READ
                    //Hide Episode List
                    Archive.IsVisible = false;
                    //Show Read
                    Read.IsVisible = true;
                    //Hide Journal
                    LoginJournal.IsVisible = false;
                    Journal.IsVisible = false;
                    //Send info to Firebase analytics that user tapped the read tab
                    var info = new Dictionary<string, string>();
                    info.Add("channel", episode.Episode.channel_title);
                    info.Add("episode_date", episode.Episode.PubDate.ToString());
                    info.Add("episode_name", episode.title);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_read", info);
                    break;
                case 2: //JOURNAL
                    //Hide episode list
                    Archive.IsVisible = false;
                    //Hide Read
                    Read.IsVisible = false;
                    //Show Journal
                    if (GuestStatus.Current.IsGuestLogin)
                    {
                        LoginJournal.IsVisible = true;
                        Journal.IsVisible = false;
                    }
                    else
                    {
                        Journal.IsVisible = true;
                        LoginJournal.IsVisible = false;
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
        //Handle the selection of a different episode
        {
            //Load the episode
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout labelHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "labelHolder");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            labelHolder.IsVisible = true;
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            var newEp = (EpisodeViewModel)e.Item;


            ////TODO: Replace for journal?
            ////Join the journal channel
            journal.JoinRoom(newEp.Episode.PubDate);

            // Make sure we have a file to play
            if (newEp.Episode.File_name_local != null || CrossConnectivity.Current.IsConnected)
            {
                episode = (EpisodeViewModel)e.Item;

                //Bind episode data to the new episode (not the player though)
                BindControls(true, false);

                if (GlobalResources.CurrentEpisodeId != episode.Episode.id)
                {
                    //TODO: Replace for journal?
                    journal.Content = null;
                    SetVisibility(false);
                }
                else
                {
                    SetVisibility(true);
                }
                EpisodeList.SelectedItem = null;
                await SetReading();

                //Send info to Firebase analytics that user accessed and episode
                var infoJ = new Dictionary<string, string>();
                infoJ.Add("channel", episode.Episode.channel_title);
                infoJ.Add("episode_date", episode.Episode.PubDate.ToString());
                infoJ.Add("episode_name", episode.Episode.title);
                DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_selected", infoJ);
            }
            else
            {
                await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played while offline download them before going offline.", "OK");
            }
            //TODO: Set completed image
            //Completed.Image = episode.listenedToSource;
            labelHolder.IsVisible = false;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
        }

        public void OnMonthSelected(object o, EventArgs e)
        {
            TimedActions();
        }

        async void OnChannel(object o, EventArgs e)
        /* User selected a different channel */
        {
            //Wait indicator 
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            StackLayout labelHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "labelHolder");
            labelHolder.IsVisible = true;
            activity.IsVisible = true;
            activityHolder.IsVisible = true;

            //Load the episode list
            if (CrossConnectivity.Current.IsConnected || PlayerFeedAPI.GetEpisodeList((Resource)ChannelsList.SelectedItem).Count() > 0)
            {
                //Store the resource / channel
                _resource = (Resource)ChannelsList.SelectedItem;
                BackgroundImage.Source = _resource.images.backgroundTablet;

                //Load the list if episodes for the channel.
                await PlayerFeedAPI.GetEpisodes(_resource);

                //Get the list of episodes from the resource
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
                TimedActions();

                //Prep the new episode (don't play yet though0
                episode = new EpisodeViewModel(Episodes.FirstOrDefault());
                if (episode != null)
                {
                    if (episode.Episode.id != GlobalResources.CurrentEpisodeId)
                    {
                        //Prep player tab
                        BindControls(true, false);
                        SetVisibility(false);

                        //PRep reading tab
                        await SetReading();

                        //Load the journal for the episode
                        //TODO: Replace for journal?
                        journal.JoinRoom(Episodes.First().PubDate);


                    }
                    else
                    {
                        //Current episode 
                        SetVisibility(true);
                    }
                }
                else
                {
                    //TODO: Handle no episodes available
                }

                //TODO: Fix completed image
                //Completed.Image = episode.listenedToSource;
            }
            else
            {
                //No episodes available
                await DisplayAlert("Unable to get episodes for channel.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
            }
            labelHolder.IsVisible = false;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
        }

        //Go back 30 seconds
        void OnBack30(object o, EventArgs e)
        {
            player.Seek(player.CurrentPosition - 30);
        }

        //Move forward 30 seconds
        void OnForward30(object o, EventArgs e)
        {
            player.Seek(player.CurrentPosition + 30);
        }

        //Play or pause the episode
        void OnPlay(object o, EventArgs e)
        {
            if (player.IsReady)
            {
                if (player.IsPlaying) //Pause if playing
                {
                    player.Pause();
                }
                else //Play if paused
                {
                    player.Play();
                    Device.StartTimer(TimeSpan.FromSeconds(ContentConfig.Instance.options.log_position_interval), () =>
                    {
                        AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "pause", player.CurrentPosition, null, null);
                        return true;
                    });
                }
            }
            else
            {
                if (!player.Load(episode.Episode)) //Load the episode
                {
                    DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                    //TODO: Ensure nothing breaks if this happens.
                    return;
                }
                //start playback
                player.Play();
                Device.StartTimer(TimeSpan.FromSeconds(ContentConfig.Instance.options.log_position_interval), () =>
                {
                    AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "pause", player.CurrentPosition, null, null);
                    return true;
                });
            }
        }

        //Share the episode
        void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(episode.Episode.channel_code, episode.Episode.PubDate.ToString("MMddyyyy"));
        }

        //Page appears event
        protected override void OnAppearing()
        {
            base.OnAppearing();
            BindControls(true, true); //rebind controls when clicking on miniplayer
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
            ////TODO: Replace for journal?
            if (episode != null && !GuestStatus.Current.IsGuestLogin)
            {
                journal.JoinRoom(episode.Episode.PubDate);
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

        //User login
        void OnLogin(object o, EventArgs e)
        {
            Login.IsEnabled = false;
            player.Pause(); if ((string)Months.SelectedItem == "My Favorites")
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
            player.Stop();
            if (CrossConnectivity.Current.IsConnected)
            {
                var nav = new NavigationPage(new DabLoginPage(true));
                nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
                Navigation.PushModalAsync(nav);
            }
            else DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
            Login.IsEnabled = true;
        }

        //Set the reading
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

            //Initialize an episode for playback. This may fire when initially loading
            //the page if the first playback, or it may wait until they press the fake "play" button
            //to start an episode after a different one is loaded.

            Initializer.IsVisible = false; //Hide the init button


            //Load the file if not already loaded
            if (episode.Episode.id != GlobalResources.CurrentEpisodeId)
            {
                if (!player.Load(episode.Episode))
                {
                    DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                    //TODO: Ensure nothing breaks if this happens.
                    return;
                }

                //Store episode data across app
                GlobalResources.CurrentEpisodeId = (int)episode.Episode.id;
            }

            //Go to starting position
            player.Seek(episode.Episode.stop_time);

            //Bind controls for playback
            BindControls(true, true);

            //Set up journal
            ////TODO: Replace for journal?
            if (!GuestStatus.Current.IsGuestLogin)
            {
                journal.JoinRoom(episode.Episode.PubDate);
            }

            //Start playing if they pushed the play button
            if (o != null)
            {
                player.Play();
            }

            SetVisibility(true); //Adjust visibility of controls

        }


        //Bind controls to episode and player
        void BindControls(bool BindToEpisode, bool BindToPlayer)
        {
            if (BindToEpisode)
            {
                //BINDINGS TO EPISODE

                //Episode Title
                lblEpisodeTitle.BindingContext = episode;
                lblEpisodeTitle.SetBinding(Label.TextProperty, "title");

                //Channel title
                lblChannelTitle.BindingContext = episode;
                lblChannelTitle.SetBinding(Label.TextProperty, "channelTitle");

                //Episode Description
                EpDescription.BindingContext = episode;
                EpDescription.SetBinding(Label.TextProperty, "description");

                //Favorite button
                favorite.BindingContext = episode;
                favorite.SetBinding(Image.SourceProperty, "favoriteSource");
                favorite.SetBinding(AutomationProperties.NameProperty, "favoriteAccessible");
                //TODO: Add Binding for AutomationProperties.Name for favoriteAccessible

                //Completed button
                Completed.BindingContext = episode;
                Completed.SetBinding(Button.ImageProperty, "listenedToSource");
                Completed.SetBinding(AutomationProperties.NameProperty, "listenAccessible");
                //TODO: Add Binding for AutomationProperties.Name for listenAccessible

                //Journal Title
                JournalTitle.BindingContext = episode;
                JournalTitle.SetBinding(Label.TextProperty, "title");
                JournalContent.BindingContext = journal;
                JournalContent.SetBinding(Editor.TextProperty, "Content");
                JournalContent.SetBinding(Editor.IsEnabledProperty, "IsConnected");
                JournalWarning.BindingContext = journal;
                JournalWarning.SetBinding(IsVisibleProperty, "IsDisconnected");
            }

            if (BindToPlayer)
            {
                //PLAYER BINDINGS
                //Current Time
                lblCurrentPosition.BindingContext = player;
                lblCurrentPosition.SetBinding(Label.TextProperty, "CurrentPosition", BindingMode.Default, new StringConverter());

                //Total Time
                lblRemainingTime.BindingContext = player;
                lblRemainingTime.SetBinding(Label.TextProperty, "RemainingSeconds", BindingMode.Default, new StringConverter());

                //Seek bar setup
                SeekBar.BindingContext = player;
                SeekBar.SetBinding(Slider.ValueProperty, "CurrentPosition");
                SeekBar.SetBinding(Slider.MaximumProperty, "Duration");
                SeekBar.UserInteraction += (object sender, EventArgs e) =>
                {
                    player.Seek(SeekBar.Value);
                };

                //Play-Pause button
                PlayPause.BindingContext = player;
                PlayPause.SetBinding(Image.SourceProperty, "PlayPauseButtonImageBig");



            }
        }

        //TODO: Replace for journal?
        async void OnJournalChanged(object o, EventArgs e)
        {
            if (JournalContent.IsFocused)
            {
                journal.UpdateJournal(episode.Episode.PubDate, JournalContent.Text);
                if (episode.Episode.has_journal == false)
                {
                    episode.Episode.has_journal = true;
                    episode.hasJournalVisible = true;
                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, null, null, true, null);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "entryDate", null, null, null);
                }
            }
        }

        //TODO: Replace for journal?
        void OnDisconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
            JournalWarning.IsEnabled = true;
        }

        //TODO: Replace for journal?
        async void OnReconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //	DisplayAlert("Reconnected to journal server.", $"Journal changes will now be saved. {o.ToString()}", "OK");
            //});
            //JournalWarning.IsEnabled = false;
            //journal.Reconnect();
            //Debug.WriteLine($"Reconnected to journal server: {o.ToString()}");
            //await Task.Delay(1000);
            //if (!journal.IsConnected)
            //{
            //    await DisplayAlert("Unable to reconnect to journal server", "Please check your internet connection and try again.", "OK");
            //}
            //JournalWarning.IsEnabled = true;

            journal.Reconnect();
            journal.JoinRoom(episode.Episode.PubDate);

            if (journal.IsConnected)
            {
                JournalWarning.IsVisible = false;
                JournalContent.IsEnabled = true;
            }
            else
            {
                await DisplayAlert("Unable to reconnect to journal server", "Please check your internet connection and try again.", "OK");
            }
        }

        //TODO: Replace for journal?
        //void OnReconnecting(object o, EventArgs e)
        //{
        //    //Device.BeginInvokeOnMainThread(() =>
        //    //{
        //    //	DisplayAlert("Reconnecting to journal server.", $"On successful reconnection changes to journal will be saved. {o.ToString()}", "OK");
        //    //});
        //    Debug.WriteLine($"Reconnecting to journal server: {o.ToString()}");
        //}

        ////TODO: Replace for journal?
        //void OnRoom_Error(object o, EventArgs e)
        //{
        //    //Device.BeginInvokeOnMainThread(() =>
        //    //{
        //    //	DisplayAlert("A room error has occured.", $"The journal server has sent back a room error. Error: {o.ToString()}", "OK");
        //    //});
        //    Debug.WriteLine($"Room Error: {o.ToString()}");
        //}

        ////TODO: Replace for journal?
        //void OnAuth_Error(object o, EventArgs e)
        //{
        //    //Device.BeginInvokeOnMainThread(() =>
        //    //{
        //    //	DisplayAlert("An auth error has occured.", $"The journal server has sent back an authentication error.  Try logging back in.  Error: {o.ToString()}", "OK");
        //    //});
        //    Debug.WriteLine($"Auth Error: {o.ToString()}");
        //}

        ////TODO: Replace for journal?
        //void OnJoin_Error(object o, EventArgs e)
        //{
        //    //Device.BeginInvokeOnMainThread(() =>
        //    //{
        //    //	DisplayAlert("A join error has occured.", $"The journal server has sent back a join error. Error: {o.ToString()}", "OK");
        //    //});
        //    Debug.WriteLine($"Join error: {o.ToString()}");
        //}

        //TODO: These need replaced and linked back to journal
        //Journal was edited
        void OnEdit(object o, EventArgs e)
        {
            journal.ExternalUpdate = false;
            //JournalTracker.Current.socket.ExternalUpdate = false;
        }

        //TODO: These need replaced and linked back to journal
        //Journal editing finished?
        void OffEdit(object o, EventArgs e)
        {
            journal.ExternalUpdate = true;
            if (!journal.IsConnected)
            {
                journal.Reconnect();
            }
            //JournalTracker.Current.socket.ExternalUpdate = true;
            //if (!JournalTracker.Current.IsJoined)
            //{
            //    JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            //}
        }

        //TODO: Replace for journal?
        void OnKeyboardChanged(object o, KeyboardHelperEventArgs e)
        {
            if (journal.IsConnected)
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
            episode.favoriteVisible = !episode.favoriteVisible;
            favorite.Source = episode.favoriteSource;
            AutomationProperties.SetName(favorite, episode.favoriteAccessible);
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, null, episode.favoriteVisible, null, null);
            await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "favorite", null, null, !episode.Episode.is_favorite);
            favorite.IsEnabled = true;
            favorite.Opacity = 1;
            //EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
        }

        async void OnListListened(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            //start new

            model.listenedToVisible = !ep.is_listened_to;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, model.listenedToVisible, null, null, null);
            //await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, null, true, null, null);
            if (ep.id == episode.Episode.id)
            {
                episode.Episode.is_listened_to = !episode.Episode.is_listened_to;
                //TODO: Fix completed image
                //Completed.Image = episode.listenedToSource;
                AutomationProperties.SetHelpText(Completed, episode.listenAccessible);
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", null, episode.Episode.is_listened_to, null);
            }
            else
            {
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listenee", null, !episode.Episode.is_listened_to, null);

            }
        }

        async void OnListFavorite(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            //start new

            model.favoriteVisible = !ep.is_favorite;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, null, model.favoriteVisible, null, null);
            //await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, null, true, null, null);
            if (ep.id == episode.Episode.id)
            {
                episode.Episode.is_favorite = !episode.Episode.is_favorite;
                //TODO: Fix completed image
                //Completed.Image = episode.listenedToSource;
                //AutomationProperties.SetHelpText(Completed, episode.favoriteAccessible);
                favorite.Source = episode.favoriteSource;
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "favorite", null, null, episode.Episode.is_favorite);

            }
            else
            {
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "favorite", null, null, !episode.Episode.is_favorite);

            }
        }

        async void OnListened(object o, EventArgs e)
        {
            //if (episode.Episode.is_listened_to == true)
            //{
            //    //check opposite of this 
            //    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, false, null, null, null);
            //    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", episode.Episode.stop_time, false);
            //}
            //else
            //{
            //    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, true, null, null, null);
            //    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", episode.Episode.stop_time, true);
            //}
            //episode.listenedToVisible = !episode.listenedToVisible;
            ////TODO: Fix completed image
            ////Completed.Image = episode.listenedToSource;
            //AutomationProperties.SetName(Completed, episode.listenAccessible);
            //TimedActions();
            episode.listenedToVisible = !episode.listenedToVisible;
            AutomationProperties.SetName(Completed, episode.listenAccessible);
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.listenedToVisible, null, null, null);
            await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", null, episode.Episode.is_listened_to);
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
                //TODO: Fix completed Image
                //Completed.Image = episode.listenedToSource;
            }
        }
    }
}
