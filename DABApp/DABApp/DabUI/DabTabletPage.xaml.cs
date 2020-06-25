using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabAudio;
using DABApp.DabSockets;
using DABApp.Service;
using Newtonsoft.Json;
using Plugin.Connectivity;
using Xamarin.Forms;


/* This page is the tablet player page and controls channels, episodes, and player on tablet devices
 * */

namespace DABApp
{
    public partial class DabTabletPage : DabBaseContentPage
    {
        DabPlayer player = GlobalResources.playerPodcast; //Reference to the global podcast player object
        DabGraphQlVariables variables = new DabGraphQlVariables(); //Instance used for websocket communication
        Resource _resource; //The current resource (initially passed from the channels page)
        IEnumerable<dbEpisodes> Episodes; //List of episodes for current channel
        ObservableCollection<EpisodeViewModel> list;
        string backgroundImage; //background image in the header?
        EpisodeViewModel episode;
        EpisodeViewModel previousEpisode;
        EpisodeViewModel nextEpisode;
        double original;
        private double _width; //screen width
        private double _height; //screen height
        DabJournalService journal;
        static int currentIndex;
        int previousEpIndex;
        int nextEpIndex;
        int count;




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

            //break episodes months out into list
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                var m = MonthConverter.ConvertToFull(month);
                Months.Items.Add(m);
            }
            Months.Items.Insert(0, "All Episodes");
            Months.SelectedIndex = 0;

            //Run timed actions and subscribe to events to update them
            MessagingCenter.Subscribe<string>("Update", "Update", (obj) =>
            {
                TimedActions();
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource); //Get episodes for selected channel
            });

            //Load the specified episode
            if (Episode != null)
            {
                episode = new EpisodeViewModel(Episode);
            }
            else
            {
                if (Episodes.Count() > 0)
                {
                    episode = new EpisodeViewModel(Episodes.First());
                }
            }

            //Bind to the active episode
            Favorite.BindingContext = episode;
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
                if (episode != null)
                {
                    journal.JoinRoom(episode.Episode.PubDate);
                }
            }

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
                    if (episode != null)
                    {
                        episode = new EpisodeViewModel(PlayerFeedAPI.GetEpisode(episode.Episode.id.Value));
                    }
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
                case 0: //ARCHIVE
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
                    if (episode == null && Episodes.Count() > 0)
                    {
                        episode = new EpisodeViewModel(Episodes.First());
                    }
                    if (episode != null)
                    {
                        //Send info to Firebase analytics that user tapped the read tab
                        var info = new Dictionary<string, string>();
                        info.Add("channel", episode.Episode.channel_title);
                        info.Add("episode_date", episode.Episode.PubDate.ToString());
                        info.Add("episode_name", episode.title);
                        DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_read", info);
                    }
                    else
                    {
                        Archive.Focus();
                    }
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
                    if (episode == null && Episodes.Count() > 0)
                    {
                        episode = new EpisodeViewModel(Episodes.First());
                    }
                    if (episode != null)
                    {
                        //Send info to Firebase analytics that user tapped the journal tab
                        var infoJ = new Dictionary<string, string>();
                        infoJ.Add("channel", episode.Episode.channel_title);
                        infoJ.Add("episode_date", episode.Episode.PubDate.ToString());
                        infoJ.Add("episode_name", episode.Episode.title);
                        DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_journal", infoJ);
                    }
                    break;
            }
        }

        public async void OnEpisode(object o, ItemTappedEventArgs e)
        //Handle the selection of a different episode
        {
            //Load the episode
            GlobalResources.WaitStart("Loading episode...");
            var newEp = (EpisodeViewModel)e.Item;
            currentIndex = list.IndexOf(newEp);
            previousEpIndex = currentIndex + 1;
            nextEpIndex = currentIndex - 1;
            count = list.Count();

            if (previousEpIndex < count)
            {
                previousEpisode = list.ElementAt(previousEpIndex);
                previousButton.IsEnabled = true;
            }
            else
            {
                previousEpisode = null;
                previousButton.IsEnabled = false;
            }
            if (nextEpIndex >= 0)
            {
                nextEpisode = list.ElementAt(nextEpIndex);
                nextButton.IsEnabled = true;
            }
            else
            {
                nextEpisode = null;
                nextButton.IsEnabled = false;
            }

            ////TODO: Replace for journal?
            ////Join the journal channel
            if (!GuestStatus.Current.IsGuestLogin)
            {
                journal.JoinRoom(newEp.Episode.PubDate);
            }

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
            GlobalResources.WaitStop();
        }

        public void OnMonthSelected(object o, EventArgs e)
        {
            TimedActions();
        }

        async void OnChannel(object o, EventArgs e)
        /* User selected a different channel */
        {
            //Wait indicator 
            GlobalResources.WaitStart("Loading channel...");

            List<string> emptyList = new List<string>();

            //Load the episode list
            if (CrossConnectivity.Current.IsConnected || PlayerFeedAPI.GetEpisodeList((Resource)ChannelsList.SelectedItem).Count() > 0)
            {
                //Store the resource / channel
                _resource = (Resource)ChannelsList.SelectedItem;
                BackgroundImage.Source = _resource.images.backgroundTablet;

                //Load the list if episodes for the channel.
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource);

                // send websocket message to get episodes by channel
                string lastEpisodeQueryDate = GlobalResources.GetLastEpisodeQueryDate(_resource.id);
                Debug.WriteLine($"Getting episodes by ChannelId");
                var episodesByChannelQuery = "query { episodes(date: \"" + lastEpisodeQueryDate + "\", channelId: " + _resource.id + ") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                var episodesByChannelPayload = new DabGraphQlPayload(episodesByChannelQuery, variables);
                var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", episodesByChannelPayload));
                DabSyncService.Instance.Send(JsonIn);
            }
            else
            {
                //No episodes available
                await DisplayAlert("Unable to get episodes for channel.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
            }

            GlobalResources.WaitStop();
        }

        //Go to previous episode
        async void OnPrevious(object o, EventArgs e)
        //Handle the selection of a different episode
        {
            previousButton.IsEnabled = false;
            try
            {
                player.Pause(); //should save position

                //Load the episode
                GlobalResources.WaitStart();
                var newEp = previousEpisode;
                currentIndex = currentIndex + 1;
                previousEpIndex = currentIndex + 1;
                nextEpIndex = currentIndex - 1;
                count = list.Count();

                if (previousEpIndex < count)
                {
                    previousEpisode = list.ElementAt(previousEpIndex);
                    previousButton.IsEnabled = true;
                }
                else
                {
                    previousEpisode = null;
                    previousButton.IsEnabled = false;
                }
                if (nextEpIndex >= 0)
                {
                    nextEpisode = list.ElementAt(nextEpIndex);
                    nextButton.IsEnabled = true;
                }
                else
                {
                    nextEpisode = null;
                    nextButton.IsEnabled = false;
                }

                // Make sure we have a file to play
                if (newEp.Episode.File_name_local != null || CrossConnectivity.Current.IsConnected)
                {
                    episode = newEp;
                    episode.Episode.ResetUserData();

                    //Bind episode data to the new episode (not the player though)
                    BindControls(true, false);

                    ////TODO: Replace for journal?
                    ////Join the journal channel
                    journal.JoinRoom(episode.Episode.PubDate);

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
                GlobalResources.WaitStop();
                player.Pause();
            }
            catch (Exception ex)
            {
                GlobalResources.WaitStop();
            }
        }

        //Go to next episode
        async void OnNext(object o, EventArgs e)
        //Handle the selection of a different episode
        {
            nextButton.IsEnabled = false;
            try
            {

                player.Pause();//should save position

                //Load the episode
                GlobalResources.WaitStart();
                var newEp = nextEpisode;
                currentIndex = currentIndex - 1;
                previousEpIndex = currentIndex + 1;
                nextEpIndex = currentIndex - 1;
                count = list.Count();


                if (previousEpIndex < count)
                {
                    previousEpisode = list.ElementAt(previousEpIndex);
                    previousButton.IsEnabled = true;
                }
                else
                {
                    previousEpisode = null;
                    previousButton.IsEnabled = false;
                }
                if (nextEpIndex >= 0)
                {
                    nextEpisode = list.ElementAt(nextEpIndex);
                    nextButton.IsEnabled = true;
                }
                else
                {
                    nextEpisode = null;
                    nextButton.IsEnabled = false;
                }



                // Make sure we have a file to play
                if (newEp.Episode.File_name_local != null || CrossConnectivity.Current.IsConnected)
                {
                    episode = newEp;
                    episode.Episode.ResetUserData();

                    //Bind episode data to the new episode (not the player though)
                    BindControls(true, false);

                    ////TODO: Replace for journal?
                    ////Join the journal channel
                    journal.JoinRoom(episode.Episode.PubDate);

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
                GlobalResources.WaitStop();
                player.Pause();
            }
            catch (Exception ex)
            {
                GlobalResources.WaitStop();
            }

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
            if (currentIndex == 0)
            {
                nextButton.Opacity = .5;
                nextButton.IsEnabled = false;
            }
            if (currentIndex == count - 1)
            {
                previousButton.Opacity = .5;
                previousButton.IsEnabled = false;
            }
            if (player.IsReady)
            {
                if (player.IsPlaying) //Pause if playing
                {
                    player.Pause();
                }
                else //Play if paused
                {
                    player.Play();

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
            }
            Device.StartTimer(TimeSpan.FromSeconds(ContentConfig.Instance.options.log_position_interval), () =>
            {
                AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "pause", player.CurrentPosition, null, null);
                return true;
            });
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

            OnRefresh(null, null); //load episodes

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
            GlobalResources.LogoffAndResetApp();
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
            try
            {

                if (episode == null && Episodes.Count() > 0)
                {
                    episode = new EpisodeViewModel(Episodes.First());
                    currentIndex = 0;
                    count = list.Count();
                    previousEpisode = list.ElementAt(currentIndex + 1);
                    previousButton.Opacity = 0;
                    nextEpisode = null;
                    nextButton.IsEnabled = false;
                    nextButton.Opacity = .5;
                }
                //Load the file if not already loaded
                if (episode != null)
                {
                    Initializer.IsVisible = false; //Hide the init button

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
                    player.Seek(episode.Episode.UserData.CurrentPosition);

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

                else
                {
                    DisplayAlert("One second", "We're loading your episodes", "Ok");
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("One second", "We're loading your episodes", "Ok");
            }
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

                //Episode Notes
                EpNotes.BindingContext = episode;
                EpNotes.SetBinding(Label.TextProperty, "notes");

                //Favorite button
                Favorite.BindingContext = episode;
                Favorite.SetBinding(Image.SourceProperty, "favoriteSource");
                Favorite.SetBinding(AutomationProperties.NameProperty, "favoriteAccessible");
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

                if (Device.RuntimePlatform == "Android")
                {
                    SeekBar.TouchUp += (object sender, EventArgs e) =>
                    {
                        player.Seek(SeekBar.Value);
                    };
                    SeekBar.TouchDown += (object sender, EventArgs e) =>
                    {
                        player.Seek(SeekBar.Value);
                    };
                }

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
                if (JournalContent.Text.Length == 0)
                {
                    episode.Episode.UserData.HasJournal = false;
                    episode.HasJournal = false;

                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, false, null);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "entryDate", null, null, null, true);
                }
                else if (episode.Episode.UserData.HasJournal == false && JournalContent.Text.Length > 0)
                {
                    episode.Episode.UserData.HasJournal = true;
                    episode.HasJournal = true;

                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, true, null);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "entryDate", null, null, null, null);
                }
            }

        }

        //TODO: Replace for journal?
        void OnDisconnect(object o, EventArgs e)
        {
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
            JournalWarning.IsEnabled = true;
        }

        //TODO: Replace for journal?
        async void OnReconnect(object o, EventArgs e)
        {

            journal.Reconnect();
            if (episode != null)
            {
                journal.JoinRoom(episode.Episode.PubDate);
            }
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
            if (nextEpisode == null && par == true)
                nextButton.Opacity = .5;
            else
                nextButton.Opacity = opa;
            if (previousEpisode == null && par == true)
                previousButton.Opacity = .5;
            else
                previousButton.Opacity = opa;
            Output.IsVisible = par;
            Share.IsVisible = par;
            Favorite.IsVisible = par;
            ListenedFrame.IsVisible = par;
            PlayPause.IsVisible = par;
            Initializer.IsVisible = !par;
            nextButton.IsVisible = true;
            previousButton.IsVisible = true;
        }



        async void OnListened(object o, EventArgs e)
        {
            episode.IsListenedTo = !episode.IsListenedTo;
            AutomationProperties.SetName(Completed, episode.listenAccessible);
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null);
            await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", null, episode.Episode.UserData.IsListenedTo);
        }



        async void OnFavorite(object o, EventArgs e)
        {
            episode.IsFavorite = !episode.IsFavorite;
            AutomationProperties.SetName(Favorite, episode.favoriteAccessible);
            await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null);
            await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "favorite", null, null, episode.Episode.UserData.IsFavorite);
        }

        async void OnListFavorite(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            //start new

            model.IsFavorite = !ep.UserData.IsFavorite;

            if (episode == null && Episodes.Count() > 0)
            {
                episode = new EpisodeViewModel(Episodes.First());
            }
            if (episode != null)
            {

                if (ep.id == episode.Episode.id)
                {
                    episode.Episode.UserData.IsFavorite = model.IsFavorite;
                    Favorite.Source = episode.favoriteSource;
                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "favorite", null, null, model.IsFavorite);
                }
                else
                {
                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                    await AuthenticationAPI.CreateNewActionLog((int)ep.id, "favorite", null, null, model.IsFavorite);
                }
            }
        }

        async void OnListListened(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            //start new

            model.IsListenedTo = !ep.UserData.IsListenedTo;
            if (episode == null && Episodes.Count() > 0)
            {
                episode = new EpisodeViewModel(Episodes.First());
            }
            if (episode != null)
            {
                if (ep.id == episode.Episode.id)
                {
                    episode.Episode.UserData.IsListenedTo = model.IsListenedTo;
                    //TODO: Fix completed image
                    Completed.Image = (Xamarin.Forms.FileImageSource)episode.listenedToSource;

                    AutomationProperties.SetHelpText(Completed, episode.listenAccessible);
                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, "listened", null, model.IsListenedTo, null);
                }
                else
                {
                    await PlayerFeedAPI.UpdateEpisodeProperty((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                    await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", null, model.IsListenedTo, null);
                }
            }
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
            btnRefresh.RotateTo(360, 2000).ContinueWith(x => btnRefresh.RotateTo(0, 0)); ; //don't await this.
            DateTime lastRefreshDate = Convert.ToDateTime(GlobalResources.GetLastRefreshDate(_resource.id));
            int pullToRefreshRate = GlobalResources.PullToRefreshRate;
            bool ok;
#if DEBUG
            ok = true;
#else
            ok = DateTime.Now.Subtract(lastRefreshDate).TotalMinutes >= pullToRefreshRate;
#endif

            if (ok)
            {
                GlobalResources.WaitStart("Refreshing episodes...");


                DateTime queryDate = GlobalResources.DabMinDate.ToUniversalTime();
                string minQueryDate = queryDate.ToString("o");

                //send websocket message to get episodes by channel
                DabGraphQlVariables variables = new DabGraphQlVariables();
                Debug.WriteLine($"Getting episodes by ChannelId");
                var episodesByChannelQuery = "query { episodes(date: \"" + minQueryDate + "\", channelId: " + _resource.id + ") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                var episodesByChannelPayload = new DabGraphQlPayload(episodesByChannelQuery, variables);
                string JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", episodesByChannelPayload));
                DabSyncService.Instance.Send(JsonIn);


                await AuthenticationAPI.PostActionLogs(false);
                await AuthenticationAPI.GetMemberData();
                if (episode == null && Episodes.Count() > 0)
                {
                    //pick the first episode
                    episode = new EpisodeViewModel(Episodes.First());
                    currentIndex = 0;
                    count = list.Count();
                    previousEpisode = list.ElementAt(currentIndex + 1);
                    nextEpisode = null;
                    nextButton.IsEnabled = false;
                }
                else if (episode != null)
                {
                    //use a specific episode
                    episode = new EpisodeViewModel(PlayerFeedAPI.GetEpisode(episode.Episode.id.Value));
                }
                else
                {
                    //no episode available yet.
                }
                TimedActions();

                GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();
                GlobalResources.SetLastRefreshDate(_resource.id);

                GlobalResources.WaitStop();
            }

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
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    EpisodeList.ItemsSource = list = new ObservableCollection<EpisodeViewModel>(Episodes
                    .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Months.Items[Months.SelectedIndex].Substring(0, 3))
                    .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.UserData.IsFavorite : true)
                    .Where(x => _resource.filter == EpisodeFilters.Journal ? x.UserData.HasJournal : true)
                    .Select(x => new EpisodeViewModel(x)));

                    if (episode != null)
                    {
                        Favorite.Source = episode.favoriteSource;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

            });
        }
    }
}
