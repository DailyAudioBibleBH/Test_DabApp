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
        EpisodeViewModel Episode;
        string backgroundImage;
        bool IsGuest;
        static double original;
        dbEpisodes _episode;

        public DabPlayerPage(dbEpisodes episode, Reading Reading)
        {
            InitializeComponent();
            SegControl.ValueChanged += Handle_ValueChanged;
            if (!GuestStatus.Current.IsGuestLogin)
            {
                JournalTracker.Current.Join(episode.PubDate.ToString("yyyy-MM-dd"));
            }

            IsGuest = GuestStatus.Current.IsGuestLogin;
            Episode = new EpisodeViewModel(episode);
            _episode = episode;

            //Show or hide player controls
            if (AudioPlayer.Instance.CurrentEpisodeId == 0)
            {
                //first episode being played, go ahead and initialize
                OnInitialized(null, null);
            }
            if (episode.id != AudioPlayer.Instance.CurrentEpisodeId)
            {
                SeekBar.Opacity = 0;
                TimeStrings.Opacity = 0;
                PlayerControls.VerticalOptions = LayoutOptions.StartAndExpand;
                //Output.Opacity = 0;
                PlayPause.IsVisible = false;
                backwardButton.Opacity = 0;
                forwardButton.Opacity = 0;
                //Share.Opacity = 0;
                Initializer.IsVisible = true;
                //Favorite.Opacity = 0;
            }

            //AudioPlayer.Instance.ShowPlayerBar = false;
            SeekBar.Value = AudioPlayer.Instance.CurrentTime;
            DabViewHelper.InitDabForm(this);
            backgroundImage = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == episode.channel_title).images.backgroundPhone;
            BackgroundImage.Source = backgroundImage;
            BindingContext = episode;
            //Date.Text = $"{episode.PubMonth} {episode.PubDay.ToString()} {episode.PubYear.ToString()}";
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];
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
            Completed.BindingContext = Episode;
            if (GlobalResources.Instance.IsiPhoneX)
                Listen.Margin = new Thickness(0, 0, 0, 16);

            //Send info to Firebase analytics that user accessed and episode
            var infoJ = new Dictionary<string, string>();
            infoJ.Add("channel", episode.channel_title);
            infoJ.Add("episode_date", episode.PubDate.ToString());
            infoJ.Add("episode_name", episode.description);
            DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_selected", infoJ);

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
                AudioPlayer.Instance.SetAudioFile(Episode.Episode);
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
                    Listen.IsVisible = true;
                    BackgroundImage.IsVisible = true;
                    break;
                case 1:
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
                    info.Add("episode_name", _episode.description);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_read", info);
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
                    //Divider.IsVisible = true;

                    //Send info to Firebase analytics that user tapped the journal tab
                    var infoJ = new Dictionary<string, string>();
                    infoJ.Add("channel", _episode.channel_title);
                    infoJ.Add("episode_date", _episode.PubDate.ToString());
                    infoJ.Add("episode_name", _episode.description);
                    DependencyService.Get<IAnalyticsService>().LogEvent("player_episode_journal", infoJ);
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
            SegControl.SelectedSegment = 0;
        }

        void OnLogin(object o, EventArgs e)
        {
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
            if (JournalContent.IsFocused)//Making sure to update the journal only when the user is using the TextBox so that the server isn't updating itself.
            {
                JournalTracker.Current.Update(Episode.Episode.PubDate.ToString("yyyy-MM-dd"), JournalContent.Text);
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
                JournalTracker.Current.Join(Episode.Episode.PubDate.ToString("yyyy-MM-dd"));
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
            //(base.SlideMenu as DabMenuView).ChangeAvatar();
            //base.SlideMenu = new DabMenuView();
        }

        void OnInitialized(object o, EventArgs e)
        {
            Initializer.IsVisible = false;
            AudioPlayer.Instance.SetAudioFile(Episode.Episode);

            if (o != null)
            {
                //Start playing if they pushed the play button
                AudioPlayer.Instance.Play();
            }
            //Show controls
            SeekBar.Opacity = 1;
            TimeStrings.Opacity = 1;
            PlayerControls.VerticalOptions = LayoutOptions.StartAndExpand;
            //Output.Opacity = 1;
            PlayPause.IsVisible = true;
            backwardButton.Opacity = 1;
            forwardButton.Opacity = 1;
            //Share.Opacity = 1;
            //Favorite.Opacity = 1;
        }

        void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(Episode.Episode.channel_code, Episode.Episode.id.ToString());
        }

        void OnDisconnect(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("Disconnected from journal server.", $"For journal changes to be saved you must be connected to the server.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
        }

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

        void OnReconnecting(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("Reconnecting to journal server.", $"On successful reconnection changes to journal will be saved. {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Reconnecting to journal server: {o.ToString()}");
        }

        void OnRoom_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("A room error has occured.", $"The journal server has sent back a room error. Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Room Error: {o.ToString()}");
        }

        void OnAuth_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("An auth error has occured.", $"The journal server has sent back an authentication error.  Try logging back in.  Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Auth Error: {o.ToString()}");
        }

        void OnJoin_Error(object o, EventArgs e)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            //  DisplayAlert("A join error has occured.", $"The journal server has sent back a join error. Error: {o.ToString()}", "OK");
            //});
            Debug.WriteLine($"Join error: {o.ToString()}");
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
            }
        }

        async void OnPlayerFailure(object o, EventArgs e)
        {
            await DisplayAlert("Audio Playback has stopped.", "If you are currently streaming this may be due to a loss of or poor internet connectivity.  Please check your connection and try again.", "OK");
        }

        void OnFavorite(object o, EventArgs e)
        {
            Episode.Episode.is_favorite = !Episode.Episode.is_favorite;
            Favorite.Image = Episode.favoriteSource;
            AutomationProperties.SetName(Favorite, Episode.favoriteAccessible);
            PlayerFeedAPI.UpdateEpisodeProperty((int)Episode.Episode.id, "is_favorite");
            AuthenticationAPI.CreateNewActionLog((int)Episode.Episode.id, "favorite", Episode.Episode.stop_time, null, Episode.Episode.is_favorite);
        }

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
            Completed.Image = Episode.listenedToSource;
            AutomationProperties.SetName(Completed, Episode.listenAccessible);
        }
    }
}
