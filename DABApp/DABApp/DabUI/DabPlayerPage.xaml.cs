using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SlideOverKit;
using Xamarin.Forms;
using TEditor;
using System.Threading.Tasks;
using Plugin.Connectivity;
using DABApp.DabAudio;

namespace DABApp
{
    public partial class DabPlayerPage : DabBaseContentPage
    {
        DabPlayer player = GlobalResources.playerPodcast;
        EpisodeViewModel Episode;
        string backgroundImage;
        bool IsGuest;
        static double original;
        dbEpisodes _episode;

        public DabPlayerPage(dbEpisodes episode, Reading Reading)
        {
            InitializeComponent();

            //Prep variables needed
            IsGuest = GuestStatus.Current.IsGuestLogin;
            Episode = new EpisodeViewModel(episode);
            _episode = episode;

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

            //Connect to the journal
            JournalTracker.Current.socket.Disconnect += OnDisconnect;
            JournalTracker.Current.socket.Reconnecting += OnReconnecting;
            JournalTracker.Current.socket.Room_Error += OnRoom_Error;
            JournalTracker.Current.socket.Auth_Error += OnAuth_Error;
            JournalTracker.Current.socket.Join_Error += OnJoin_Error;
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
                }
            }
            else
            {
                //TODO: Use local file name or URL, depending on if downloaded
                player.Load(Episode.Episode.url);
                player.Play();
            }
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

        //Select a tab at the top of the screen
        void Handle_ValueChanged(object sender, System.EventArgs e)
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

        //Journal data changed outside the app
        void OnJournalChanged(object o, EventArgs e)
        {
            if (JournalContent.IsFocused)//Making sure to update the journal only when the user is using the TextBox so that the server isn't updating itself.
            {
                JournalTracker.Current.Update(Episode.Episode.PubDate.ToString("yyyy-MM-dd"), JournalContent.Text);
            }
        }

        //Journal was edited
        void OnEdit(object o, EventArgs e)
        {
            JournalTracker.Current.socket.ExternalUpdate = false;
        }

        //Journal editing finished?
        void OffEdit(object o, EventArgs e)
        {
            JournalTracker.Current.socket.ExternalUpdate = true;
            if (!JournalTracker.Current.IsJoined)
            {
                JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }
        }

        //Form appears
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Device.RuntimePlatform == "iOS")
            {
                //Set up padding for the journal tab with the keyboard
                int paddingMulti = JournalTracker.Current.IsConnected ? 4 : 6;
                JournalContent.HeightRequest = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
                original = Content.Height - JournalTitle.Height - SegControl.Height - Journal.Padding.Bottom * paddingMulti;
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
            if (!GuestStatus.Current.IsGuestLogin && !JournalTracker.Current.IsJoined)
            {
                JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }
        }

        void BindControls(bool BindToEpisode, bool BindToPlayer)
        {
            if (BindToEpisode)
            {
                //BINDINGS TO EPISODE

                //Episode Title
                lblTitle.BindingContext = Episode;
                lblTitle.SetBinding(Label.TextProperty, "title");

                //Channel TItle
                lblChannelTitle.BindingContext = Episode;
                lblChannelTitle.SetBinding(Label.TextProperty, "channelTitle");

                //Episode Description
                lblDescription.BindingContext = Episode;
                lblDescription.SetBinding(Label.TextProperty, "description");

                //Favorite button
                Favorite.BindingContext = Episode;
                Favorite.SetBinding(Button.ImageProperty, "favoriteSource");
                //TODO: Add Binding for AutomationProperties.Name for favoriteAccessible

                //Completed button
                Completed.BindingContext = Episode;
                Completed.SetBinding(Button.ImageProperty, "listenedToSource");
                //TODO: Add Binding for AutomationProperties.Name for listenAccessible

                //Journal Title
                JournalTitle.BindingContext = Episode;
                JournalTitle.SetBinding(Label.TextProperty, "title");
            }

            if (BindToPlayer)
            {
                //BINDINGS TO PLAYER

                //Current Time
                lblCurrentTime.BindingContext = player;
                //TODO: Add 'stringer' converter
                lblCurrentTime.SetBinding(Label.TextProperty, "CurrentPosition", BindingMode.Default, new StringConverter());

                //Total Time
                lblTotalTime.BindingContext = player;
                //TODO: Add 'stringer' converter
                lblTotalTime.SetBinding(Label.TextProperty, "Duration", BindingMode.Default, new StringConverter());

                //Seek bar setup
                SeekBar.BindingContext = player;
                SeekBar.SetBinding(Slider.ValueProperty, "CurrentPosition");
                SeekBar.SetBinding(Slider.MaximumProperty, "Duration");
                SeekBar.UserInteraction += (object sender, EventArgs e) => player.Seek(SeekBar.Value);

                //Play-Pause button
                PlayPause.BindingContext = player;
                PlayPause.SetBinding(Image.SourceProperty, "PlayPauseButtonImageBig");
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
                //TODO: Use local file name or URL, depending on if downloaded
                player.Load(Episode.Episode.File_name);
                GlobalResources.CurrentEpisodeId = (int)Episode.Episode.id;
            }

            //Goto the starting position of the episode
            player.Seek(Episode.Episode.stop_time);

            //Bind controls for playback
            BindControls(true, true);

            SegControl.ValueChanged += Handle_ValueChanged;
            if (!GuestStatus.Current.IsGuestLogin)
            {
                JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
            }

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
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(Episode.Episode.channel_code, Episode.Episode.id.ToString());
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
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("Reconnected to journal server.", $"Journal changes will now be saved. {o.ToString()}", "OK");
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
            }
        }

        //Player failed
        //TODO: Does this even run anymore?
        async void OnPlayerFailure(object o, EventArgs e)
        {
            await DisplayAlert("Audio Playback has stopped.", "If you are currently streaming this may be due to a loss of or poor internet connectivity.  Please check your connection and try again.", "OK");
        }

        //User favorites (or unfavorites) an episode
        void OnFavorite(object o, EventArgs e)
        {
            Episode.favoriteVisible = !Episode.favoriteVisible;
            //Episode.Episode.is_favorite = !Episode.Episode.is_favorite;
            //TODO: Set favorite image
            //Favorite.Image = Episode.favoriteSource;
            AutomationProperties.SetName(Favorite, Episode.favoriteAccessible);
            PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id, "is_favorite");
            AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "favorite", Episode.Episode.stop_time, null, Episode.Episode.is_favorite);
        }

        //User listens to (or unlistens to) an episode
        void OnListened(object o, EventArgs e)
        {
            if (Episode.Episode.is_listened_to == "listened")
            {
                Episode.Episode.is_listened_to = "";
                PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id, "");
                AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "listened", Episode.Episode.stop_time, "");
            }
            else
            {
                Episode.Episode.is_listened_to = "listened";
                PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id);
                AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "listened", Episode.Episode.stop_time, "listened");
            }
            //TODO: Set completed image
            //Completed.Image = Episode.listenedToSource;
            AutomationProperties.SetName(Completed, Episode.listenAccessible);
        }
    }
}
