using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;
using System.Threading.Tasks;
using Plugin.Connectivity;
using DABApp.DabAudio;
using DABApp.DabSockets;
using SQLite;
using System.Collections.ObjectModel;

namespace DABApp
{
    public partial class DabPlayerPage : DabBaseContentPage
    {
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors
        ObservableCollection<EpisodeViewModel> list;
        DabPlayer player = GlobalResources.playerPodcast;
        EpisodeViewModel Episode;
        EpisodeViewModel nextEpisode;
        EpisodeViewModel previousEpisode;
        string backgroundImage;
        bool IsGuest;
        static double original;
        dbEpisodes _episode;
        DabEpisodesPage dabEpisodes;
        DabJournalService journal;

        public DabPlayerPage(dbEpisodes episode, Reading Reading)
        {
            InitializeComponent();

            //Prep variables needed
            IsGuest = GuestStatus.Current.IsGuestLogin;
            Episode = new EpisodeViewModel(episode);

            _episode = episode;

            GetNextPreviousEpisodes(Episode);

            //Prepare an empty journal object (needed early for binding purposes)
            journal = new DabJournalService();

            if (GlobalResources.Instance.IsiPhoneX)
            {
                iPhoneXLayout.Margin = new Thickness(0, 0, 0, -20);
                iPhoneXLayout.HeightRequest += 80;
                //footerLayout.Padding = new Thickness(0, 0, 0, -8);
                footerLayout.BackgroundColor = Color.Transparent;
            }
            //Show or hide player controls

            //first episode being played, bind controls to episode and player
            if (GlobalResources.CurrentEpisodeId == 0)
            {
                OnInitialized(null, null);
            }
            //Same episode already being played - bind controls to episode and player
            else if (episode.id == GlobalResources.CurrentEpisodeId)
            {
                BindControls(true, true);
            }
            //New episode (don't initiaze it yet and only bind to the episode, not the player)
            else if (episode.id != GlobalResources.CurrentEpisodeId)
            {
                BindControls(true, false);

                SeekBar.Opacity = 0;
                TimeStrings.Opacity = 0;
                PlayerControls.VerticalOptions = LayoutOptions.StartAndExpand;
                PlayPause.IsVisible = false;
                backwardButton.Opacity = 0;
                forwardButton.Opacity = 0;
                Initializer.IsVisible = true;
            }

            DabViewHelper.InitDabForm(this);
            backgroundImage = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == episode.channel_title).images.backgroundPhone;
            BackgroundImage.Source = backgroundImage;
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];

            /* Set up other tabs (segmented control) */
            SegControl.ValueChanged += Handle_SegControlValueChanged;

            //Update properties of the reading
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

            //Connect to the journal and set up events
            journal.InitAndConnect();
            journal.JoinRoom(episode.PubDate);

            if (Device.RuntimePlatform == "iOS")
            {
                KeyboardHelper.KeyboardChanged += OnKeyboardChanged;
            }

            //Link MarkDown button to external link
            var tapper = new TapGestureRecognizer();
            tapper.Tapped += (sender, e) =>
            {
                Device.OpenUri(new Uri("https://en.wikipedia.org/wiki/Markdown"));
            };
            AboutFormat.GestureRecognizers.Add(tapper);

            //Add some margin to bottom of the page for iPhoneX
            if (GlobalResources.Instance.IsiPhoneX)
                Listen.Margin = new Thickness(0, 0, 0, 16);

            //Send info to Firebase analytics that user accessed and episode
            var infoJ = new Dictionary<string, string>();
            infoJ.Add("channel", episode.channel_title);
            infoJ.Add("episode_date", episode.PubDate.ToString());
            infoJ.Add("episode_name", episode.title);
            DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_selected", infoJ);

            MessagingCenter.Subscribe<string>("dabapp", "SocketConnected", (obj) =>
            {
                int paddingMulti = journal.IsConnected ? 4 : 8;
                JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
            });

            MessagingCenter.Subscribe<string>("dabapp", "SocketDisconnected", (obj) =>
            {
                int paddingMulti = journal.IsConnected ? 4 : 8;
                JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
            });
            MessagingCenter.Subscribe<string>("dabapp", "EpisodeDataChanged", (obj) =>
            {
                Device.BeginInvokeOnMainThread(() =>
               {
                   BindControls(true, true);
               });

            });

            //Play-Pause button binding
            //Moved here to take away flicker when favoriting and marking an episode as listened to 
            PlayPause.BindingContext = player;
            PlayPause.SetBinding(Image.SourceProperty, "PlayPauseButtonImageBig");
        }

        async void GetNextPreviousEpisodes(EpisodeViewModel _episode)
        {
            var savedEps = await adb.Table<dbEpisodes>().ToListAsync();
            list = new ObservableCollection<EpisodeViewModel>(savedEps.Select(x => new EpisodeViewModel(x)));

            nextEpisode = list.OrderBy(x => x.Episode.PubDate).FirstOrDefault(x => x.Episode.PubDate > Episode.Episode.PubDate && x.Episode.id != Episode.Episode.id && Episode.channelTitle == x.channelTitle);
            previousEpisode = list.OrderBy(x => x.Episode.PubDate).LastOrDefault(x => x.Episode.PubDate < Episode.Episode.PubDate && x.Episode.id != Episode.Episode.id && Episode.channelTitle == x.channelTitle);
            if (nextEpisode == null)
                nextButton.Opacity = .5;
            else
                nextButton.Opacity = 1;
            if (previousEpisode == null)
                previousButton.Opacity = .5;
            else
                previousButton.Opacity = 1;
        }

        //Play or Pause the episode (not the same as the init play button)
        void OnPlay(object o, EventArgs e)
        {
            if (player.IsReady)
            {
                if (player.IsPlaying)
                {
                    player.Pause();
                }
                else
                {
                    player.Play();
                    Device.StartTimer(TimeSpan.FromSeconds(ContentConfig.Instance.options.log_position_interval), () =>
                    {
                        AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "pause", player.CurrentPosition, null, null);
                        return true;
                    });
                }
            }
            else
            {
                if (player.Load(Episode.Episode))
                {
                    player.Play();
                    Device.StartTimer(TimeSpan.FromSeconds(ContentConfig.Instance.options.log_position_interval), () =>
                    {
                        AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "pause", player.CurrentPosition, null, null);
                        return true;
                    });
                }
                else
                {
                    DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                }

            }
        }

        //Go to previous episode
        void OnPrevious(object o, EventArgs e)
        {
            if (previousEpisode != null)
            {
                Episode = previousEpisode;
            }
            GetNextPreviousEpisodes(Episode);
            player.Load(Episode.Episode);
            BindControls(true, true);
        }

        //Go to next episode
        void OnNext(object o, EventArgs e)
        {
            if (nextEpisode != null)
            {
                Episode = nextEpisode;
            }
            GetNextPreviousEpisodes(Episode);
            player.Load(Episode.Episode);
            BindControls(true, true);
        }

        //Go back 30 seconds
        void OnBack30(object o, EventArgs e)
        {
            player.Seek(player.CurrentPosition - 30);
        }

        //Go forward 30 seconds
        void OnForward30(object o, EventArgs e)
        {
            player.Seek(player.CurrentPosition + 30);
        }
        //Initialize an episode and bind all related controls

        //Select a tab at the top of the screen
        void Handle_SegControlValueChanged(object sender, System.EventArgs e)
        {
            switch (SegControl.SelectedSegment)
            {
                case 0:
                    //Listen tab
                    Read.IsVisible = false;
                    if (GuestStatus.Current.IsGuestLogin)
                    {
                        LoginJournal.IsVisible = false;
                    }
                    else
                    {
                        Journal.IsVisible = false;
                    }
                    Listen.IsVisible = true;
                    BackgroundImage.IsVisible = true;
                    break;

                case 1:
                    //Read tab
                    Listen.IsVisible = false;
                    if (GuestStatus.Current.IsGuestLogin)
                    {
                        LoginJournal.IsVisible = false;
                    }
                    else
                    {
                        Journal.IsVisible = false;
                    }
                    Read.IsVisible = true;
                    BackgroundImage.IsVisible = false;

                    //Send info to Firebase analytics that user tapped the read tab
                    var info = new Dictionary<string, string>();
                    info.Add("channel", _episode.channel_title);
                    info.Add("episode_date", _episode.PubDate.ToString());
                    info.Add("episode_name", _episode.title);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_read", info);
                    break;

                case 2:
                    //Journal tab
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
                    //Divider.IsVisible = true;

                    //Send info to Firebase analytics that user tapped the journal tab
                    var infoJ = new Dictionary<string, string>();
                    infoJ.Add("channel", _episode.channel_title);
                    infoJ.Add("episode_date", _episode.PubDate.ToString());
                    infoJ.Add("episode_name", _episode.title);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_journal", infoJ);
                    break;
            }
        }


        void OnPodcast(object o, EventArgs e)
        {
            //Go to the first tab
            //TODO: Why is this even here?
            SegControl.SelectedSegment = 0;
        }

        void OnLogin(object o, EventArgs e)
        //Log the user in for login specific usage
        {
            Login.IsEnabled = false;
            player.Stop();
            if (CrossConnectivity.Current.IsConnected)
            {
                var nav = new NavigationPage(new DabLoginPage(true));
                nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                Navigation.PushModalAsync(nav);
            }
            else DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
            Login.IsEnabled = true;
        }


        //TODO: These need replaced and linked back to journal
        //Journal data changed outside the app
        async void OnJournalChanged(object o, EventArgs e)
        {
            if (JournalContent.IsFocused)//Making sure to update the journal only when the user is using the TextBox so that the server isn't updating itself.
            {
                journal.UpdateJournal(Episode.Episode.PubDate, JournalContent.Text);
                //JournalTracker.Current.Update(Episode.Episode.PubDate.ToString("yyyy-MM-dd"), JournalContent.Text);
                if (Episode.Episode.UserData.HasJournal == false)
                {
                    Episode.Episode.UserData.HasJournal = true;
                    Episode.HasJournal = true;
                    await PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id, null, null, true, null);
                    await AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "entryDate", null, null, null);
                }
            }
        }

        //TODO: These need replaced and linked back to journal
        //Journal was edited
        void OnEdit(object o, EventArgs e)
        {
            journal.ExternalUpdate = false;
            //JournalTracker.Current.socket.ExternalUpdate = false;
        }

        //TODO: These need replaced and linked back to journal
        //Journal editing finished?
        async void OffEdit(object o, EventArgs e)
        {
            journal.ExternalUpdate = true;
            if (!journal.IsConnected)
            {
                journal.Reconnect();
            }
        }

        //Form appears
        protected override void OnAppearing()
        {
            base.OnAppearing();

            //TODO: Put this back in for journal
            //Set up padding for the journal tab with the keyboard
            int paddingMulti = journal.IsConnected ? 4 : 8;
            JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
            original = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;

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
            //TODO: Replace for journal?
            if (!GuestStatus.Current.IsGuestLogin && !journal.IsConnected)
            {
                journal.Reconnect();
                //JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }
        }

        void BindControls(bool BindToEpisode, bool BindToPlayer)
        {
            if (BindToEpisode)
            {
                //BINDINGS TO EPISODE

                //get a fresh reference to the episode
                dbEpisodes ep = PlayerFeedAPI.GetEpisode(Episode.Episode.id.Value);
                Episode = new EpisodeViewModel(ep);


                //Episode Title
                lblTitle.BindingContext = Episode;
                lblTitle.SetBinding(Label.TextProperty, "title");

                //Channel TItle
                lblChannelTitle.BindingContext = Episode;
                lblChannelTitle.SetBinding(Label.TextProperty, "channelTitle");

                //Episode Description
                lblDescription.BindingContext = Episode;
                lblDescription.SetBinding(Label.TextProperty, "description");

                //Episodes Notes
                lblNotes.BindingContext = Episode;
                lblNotes.SetBinding(Label.TextProperty, "notes");

                //Favorite button
                Favorite.BindingContext = Episode;
                Favorite.SetBinding(Button.ImageProperty, "favoriteSource");
                Favorite.SetBinding(AutomationProperties.NameProperty, "favoriteAccessible");
                //TODO: Add Binding for AutomationProperties.Name for favoriteAccessible

                //Next button
                //nextButton.BindingContext = Episode;
                //nextButton.SetBinding(Button.OpacityProperty, "nextOpacity");

                ////Previous button
                //previousButton.BindingContext = Episode;
                //previousButton.SetBinding(Button.OpacityProperty, "previousOpacity");


                //Completed button
                Completed.BindingContext = Episode;
                Completed.SetBinding(Button.ImageProperty, "listenedToSource");
                Completed.SetBinding(AutomationProperties.NameProperty, "listenAccessible");
                //TODO: Add Binding for AutomationProperties.Name for listenAccessible

                //Journal
                JournalTitle.BindingContext = Episode;
                JournalTitle.SetBinding(Label.TextProperty, "title");
                JournalContent.BindingContext = journal;
                JournalContent.SetBinding(Editor.TextProperty, "Content");
                JournalContent.SetBinding(Editor.IsEnabledProperty, "IsConnected");
                JournalWarning.BindingContext = journal;
                JournalWarning.SetBinding(IsVisibleProperty, "IsDisconnected");
            }

            if (BindToPlayer)
            {
                //BINDINGS TO PLAYER

                //Current Time
                lblCurrentTime.BindingContext = player;
                lblCurrentTime.SetBinding(Label.TextProperty, "CurrentPosition", BindingMode.Default, new StringConverter());

                //Total Time
                lblRemainingTime.BindingContext = player;
                lblRemainingTime.SetBinding(Label.TextProperty, "RemainingSeconds", BindingMode.Default, new StringConverter());

                //Seek bar setup
                SeekBar.BindingContext = player;
                SeekBar.SetBinding(Slider.ValueProperty, "CurrentPosition");
                SeekBar.SetBinding(Slider.MaximumProperty, "Duration");
                
                SeekBar.TouchUp += (object sender, EventArgs e) =>
                {
                    player.Seek(SeekBar.Value);
                };
                SeekBar.TouchDown += (object sender, EventArgs e) =>
                {
                    player.Seek(SeekBar.Value);
                };
            }
        }

        void OnInitialized(object o, EventArgs e)
        {
            //Initialize an episode for playback. This may fire when initially loading
            //the page if the first playback, or it may wait until they press the fake "play" button
            //to start an episode after a different one is loaded.

            Initializer.IsVisible = false;

            //Load the file if not already loaded.
            if (Episode.Episode.id != GlobalResources.CurrentEpisodeId)
            {
                if (!player.Load(Episode.Episode))
                {
                    DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                    //TODO: Ensure nothing breaks if this happens.
                    return;
                }
                GlobalResources.CurrentEpisodeId = (int)Episode.Episode.id;

            }

            //Goto the starting position of the episode
            player.Seek(Episode.Episode.UserData.CurrentPosition);

            //Bind controls for playback
            BindControls(true, true);

            ////TODO: Replace for journal?
            //if (!GuestStatus.Current.IsGuestLogin)
            //{
            //    JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            //}

            //Start playing if they pushed the play button
            if (o != null)
            {
                player.Play();
            }

            //Show controls
            SeekBar.Opacity = 1;
            TimeStrings.Opacity = 1;
            PlayerControls.VerticalOptions = LayoutOptions.StartAndExpand;
            PlayPause.IsVisible = true;
            backwardButton.Opacity = 1;
            forwardButton.Opacity = 1;
        }

        //Share the episode
        void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(Episode.Episode.channel_code, Episode.Episode.PubDate.ToString("MMddyyyy"));
        }

        //Journal disconnected
        void OnDisconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
        }

        //Journal Reconnected
        async void OnReconnect(object o, EventArgs e)
        {
            journal.Reconnect();
            journal.JoinRoom(Episode.Episode.PubDate);
            await Task.Delay(1000);
            if (!journal.IsConnected)
            {
                await DisplayAlert("Unable to reconnect to journal server", "Please check your internet connection and try again.", "OK");
            }
            if (journal.IsConnected)
            {
                JournalWarning.IsVisible = false;
                JournalContent.IsEnabled = true;
                int paddingMulti = journal.IsConnected ? 4 : 8;
                JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
            }
        }

        //Journal reconnecting
        void OnReconnecting(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("Reconnecting to journal server.", $"On successful reconnection changes to journal will be saved. {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Reconnecting to journal server: {o.ToString()}");
        }

        //Journal room error
        void OnRoom_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("A room error has occured.", $"The journal server has sent back a room error. Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Room Error: {o.ToString()}");
        }

        //Journal auth error
        void OnAuth_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("An auth error has occured.", $"The journal server has sent back an authentication error.  Try logging back in.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Auth Error: {o.ToString()}");
        }

        //Journal Join error
        void OnJoin_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("A join error has occured.", $"The journal server has sent back a join error. Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Join error: {o.ToString()}");
        }

        //Keyboard appears or disappears
        void OnKeyboardChanged(object o, KeyboardHelperEventArgs e)
        {
            //TODO: Replace for journal?
            if (journal.IsConnected)
            {
                spacer.HeightRequest = e.Visible ? e.Height : 0;
                if (e.IsExternalKeyboard)
                {
                    JournalContent.HeightRequest = original + 100;
                }
                else
                {
                    JournalContent.HeightRequest = e.Visible ? original - e.Height + 50 : original + 100;
                }
            }
        }

        //Player failed
        //TODO: Does this even run anymore?
        async void OnPlayerFailure(object o, EventArgs e)
        {
            await DisplayAlert("Audio Playback has stopped.", "If you are currently streaming this may be due to a loss of or poor internet connectivity.  Please check your connection and try again.", "OK");
        }

        //User favorites (or unfavorites) an episode
        async void OnFavorite(object o, EventArgs e)
        {
            Episode.IsFavorite = !Episode.IsFavorite;
            AutomationProperties.SetName(Favorite, Episode.favoriteAccessible);
            await PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id, null, Episode.IsFavorite, null, null);
            await AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "favorite", null, null, Episode.Episode.UserData.IsFavorite);
        }

        //User listens to (or unlistens to) an episode
        async void OnListened(object o, EventArgs e)
        {

            //Mark episode as listened to
            //Episode.Episode.is_listened_to = "";
            //check this
            Episode.IsListenedTo = !Episode.IsListenedTo;
            AutomationProperties.SetName(Completed, Episode.listenAccessible);
            await PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id, Episode.IsListenedTo, null, null, null);
            await AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "listened", null, Episode.Episode.UserData.IsFavorite);

            //TODO: Bind accessibiliyt text
        }

        //User listens to (or unlistens to) an episode
        void OnVisibleChanged(object o, EventArgs e)
        {
            //Switch the value of listened to
            Episode.IsListenedTo = !Episode.IsListenedTo;
        }
    }
}
