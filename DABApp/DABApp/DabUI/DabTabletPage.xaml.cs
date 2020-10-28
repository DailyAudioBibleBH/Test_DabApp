using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabAudio;
using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
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
        ObservableCollection<EpisodeViewModel> episodeObservableCollection;
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
        object source;

        #region constructor and startup methods

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

            if (Months.Items.Contains("All Episodes") == false)
            {
                Months.Items.Insert(0, "All Episodes"); //default selector
                Months.SelectedIndex = 0;

            }
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                string monthName = Helpers.MonthNameHelper.MonthNameFromNumber(month);
                if (Months.Items.Contains(monthName) == false)
                {
                    Months.Items.Add(monthName);
                }
            }

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

            //initially bind to episodes we have before trying to reload on appearing
            Refresh(EpisodeRefreshType.NoRefresh); //refresh episode list

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

            //Tap event for the MarkDownDeep link
            var tapper = new TapGestureRecognizer();
            tapper.Tapped += (sender, e) =>
            {
                Device.OpenUri(new Uri("https://en.wikipedia.org/wiki/Markdown"));
            };
            AboutFormat.GestureRecognizers.Add(tapper);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            //episodes changed event
            DabServiceEvents.EpisodesChangedEvent -= DabServiceEvents_EpisodesChangedEvent;

            //episode user data changed event
            DabServiceEvents.EpisodeUserDataChangedEvent -= DabServiceEvents_EpisodeUserDataChangedEvent;

        }

        //Page appears event
        protected async override void OnAppearing()
        {
            base.OnAppearing();

            //episodes changed event
            DabServiceEvents.EpisodesChangedEvent += DabServiceEvents_EpisodesChangedEvent;

            //episode user data changed event
            DabServiceEvents.EpisodeUserDataChangedEvent += DabServiceEvents_EpisodeUserDataChangedEvent;

            //get new episodes, if they exist -- this will also handle downloading
            await Refresh(EpisodeRefreshType.IncrementalRefresh); //refresh episode list

            BindControls(true, true); //rebind controls when clicking on miniplayer
            
            JournalContent.HeightRequest = Content.Height; //* 2 / 3; //- SegControl.Height; //- Divider.Height
       
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
                    previousEpisode = new EpisodeViewModel(Episodes.ElementAt(currentIndex + 1));// episodeObservableCollection.ElementAt(currentIndex + 1);
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

                SeekBar.UserInteraction += (object sender, EventArgs e) =>
                {
                    player.Seek(SeekBar.Value);
                };

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
                original = JournalContent.HeightRequest;
            }
        }

        #endregion

        #region refresh and download processing

        async Task<bool> DownloadEpisodes()
        {
            /*
             * download episodes 
             */
            if (_resource.availableOffline)
            {
                await PlayerFeedAPI.DownloadEpisodes();
            }

            return true;
        }

        private async void DabServiceEvents_EpisodesChangedEvent()
        {
            //new episodes added - refresh the list
            await Refresh(EpisodeRefreshType.IncrementalRefresh);
            BindControls(true, true);
        }

        private async void DabServiceEvents_EpisodeUserDataChangedEvent()
        {
            //Get fresh reference to the episode
            if (episode != null)
            {
                episode = new EpisodeViewModel(PlayerFeedAPI.GetEpisode(episode.Episode.id.Value));
            }
            //user data has changed (not episode list itself)
            await Refresh(EpisodeRefreshType.NoRefresh);
            BindControls(true, true);
        }

        async Task Refresh(EpisodeRefreshType refreshType)
        {
            /* 
             * this routine pulls any new episodes for the selected channel, 
             * updates the ui, 
             * and downloads them
             * 
             * You can have it to no refresh (sort/filter),
             * incremental refresh (look for new episodes),
             * or full refresh (go back and query all episodes)
             * 
             */

            DateTime lastRefreshDate = Convert.ToDateTime(GlobalResources.GetLastRefreshDate(_resource.id));
            DateTime minQueryDate;

            if (refreshType != EpisodeRefreshType.NoRefresh)
            {
                //refresh episodes from the server

                if (refreshType == EpisodeRefreshType.FullRefresh)
                {
                    //only let them reload everything at a rate we set.
                    int pullToRefreshRate = GlobalResources.PullToRefreshRate; //how often the user can refresh
                    if (DateTime.Now.Subtract(lastRefreshDate).TotalMinutes >= pullToRefreshRate)
                    {
                        minQueryDate = GlobalResources.DabMinDate;
                    }
                    else
                    {
                        return; //don't do anything if they've recently pulled to refresh
                    }
                }
                else
                {
                    //incremental refresh
                    minQueryDate = GlobalResources.GetLastEpisodeQueryDate(_resource.id);
                }

                //get the episodes - this routine handles resetting the date and raising events
                source = new object();
                DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Refresing episodes...", true));
                var result = await DabServiceRoutines.GetEpisodes(_resource.id, (refreshType == EpisodeRefreshType.FullRefresh));
                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
            }

            //get the rull list of episodes for the resource
            Episodes = PlayerFeedAPI.GetEpisodeList(_resource);

            //Update month list
            if (Months.Items.Contains("All Episodes") == false)
            {
                Months.Items.Insert(0, "All Episodes"); //default selector
                Months.SelectedIndex = 0;

            }
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                string monthName = Helpers.MonthNameHelper.MonthNameFromNumber(month);
                if (Months.Items.Contains(monthName) == false)
                {
                    Months.Items.Add(monthName);
                }
            }

            //sort the episodes
            if (_resource.AscendingSort)
            {
                Episodes = Episodes.OrderBy(x => x.PubDate);
            }
            else
            {
                Episodes = Episodes.OrderByDescending(x => x.PubDate);
            }

            //update the list with any filters / sorting applied
            if (Episodes.Count() > 0)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    //filter to the right list of episodes
                    ObservableCollection<EpisodeViewModel> episodeObservableCollection = new ObservableCollection<EpisodeViewModel>(Episodes
                    .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Helpers.MonthNameHelper.MonthNumberFromName(Months.Items[Months.SelectedIndex]))
                    .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.UserData.IsFavorite : true)
                    .Where(x => _resource.filter == EpisodeFilters.Journal ? x.UserData.HasJournal : true)
                    .Select(x => new EpisodeViewModel(x)).ToList());

                    if (episode != null)
                    {
                        Favorite.Source = episode.favoriteSource;
                    }

                    foreach (var item in episodeObservableCollection)
                    {
                        if (item.Episode.is_downloaded == true)
                        {
                            item.isDownloaded = true;
                            item.isNotDownloaded = false;
                            item.downloadProgress = 100;
                        }
                        else
                        {
                            item.isNotDownloaded = true;
                        }
                        if (episodeObservableCollection.IndexOf(item) >= 20)
                        {
                            break;
                        }
                    }

                    EpisodeList.ItemsSource = episodeObservableCollection;
                    //Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
                }
                );
            }

            //download any new episodes
            await DownloadEpisodes();

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
                    EpisodeList.ItemsSource = episodeObservableCollection = new ObservableCollection<EpisodeViewModel>(Episodes
                    .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Helpers.MonthNameHelper.MonthNumberFromName(Months.Items[Months.SelectedIndex]))
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

        #endregion refresh and download processing

        #region user interaction methods

        async void OnListened(object o, EventArgs e)
        {
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                episode.IsListenedTo = !episode.IsListenedTo;
                AutomationProperties.SetName(Completed, episode.listenAccessible);
                await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null);
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, DabService.ServiceActionsEnum.Listened, null, episode.Episode.UserData.IsListenedTo);
                await Refresh(EpisodeRefreshType.NoRefresh);
                BindControls(true, true);
            }
            else
            {
                //guest mode - do nothing
                await DisplayAlert("Guest Mode", "You are currently logged in as a guest. Please log in to use this feature", "OK");
            }
        }



        async void OnFavorite(object o, EventArgs e)
        {
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                episode.IsFavorite = !episode.IsFavorite;
                AutomationProperties.SetName(Favorite, episode.favoriteAccessible);
                await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null);
                await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, DabService.ServiceActionsEnum.Favorite, null, null, episode.Episode.UserData.IsFavorite);
                await Refresh(EpisodeRefreshType.NoRefresh);
                BindControls(true, true);
            }
            else
            {
                //guest mode - do nothing
                await DisplayAlert("Guest Mode", "You are currently logged in as a guest. Please log in to use this feature", "OK");
            }
        }

        async void OnListFavorite(object o, EventArgs e)
        {
            if (GuestStatus.Current.IsGuestLogin == false)
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
                        await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                        await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, DabService.ServiceActionsEnum.Favorite, null, null, model.IsFavorite);
                    }
                    else
                    {
                        await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                        await AuthenticationAPI.CreateNewActionLog((int)ep.id, DabService.ServiceActionsEnum.Favorite, null, null, model.IsFavorite);
                    }
                }
            }
            else
            {
                //guest mode - do nothing
                await DisplayAlert("Guest Mode", "You are currently logged in as a guest. Please log in to use this feature", "OK");
            }
        }

        async void OnListListened(object o, EventArgs e)
        {
            if (GuestStatus.Current.IsGuestLogin == false)
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
                        await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                        await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, DabService.ServiceActionsEnum.Listened, null, model.IsListenedTo, null);
                    }
                    else
                    {
                        await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, episode.HasJournal, null, false);
                        await AuthenticationAPI.CreateNewActionLog((int)ep.id, DabService.ServiceActionsEnum.Listened, null, model.IsListenedTo, null);
                    }
                }
            }
            else
            {
                //guest mode - do nothing
                await DisplayAlert("Guest Mode", "You are currently logged in as a guest. Please log in to use this feature", "OK");
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
                DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Refreshing episodes...", true));


                var result = await DabServiceRoutines.GetEpisodes(_resource.id);
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource);

                await Refresh(EpisodeRefreshType.FullRefresh);

                if (episode == null && Episodes.Count() > 0)
                {
                    //pick the first episode
                    episode = new EpisodeViewModel(Episodes.First());
                    currentIndex = 0;
                    previousEpisode = new EpisodeViewModel(Episodes.ElementAt(currentIndex + 1));// episodeObservableCollection.ElementAt(currentIndex + 1);
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

                GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();
                GlobalResources.SetLastRefreshDate(_resource.id);

                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
            }

            EpisodeList.IsRefreshing = false;
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
            DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Loading episode...", true));
            var newEp = (EpisodeViewModel)e.Item;
            currentIndex = episodeObservableCollection.IndexOf(newEp);
            previousEpIndex = currentIndex + 1;
            nextEpIndex = currentIndex - 1;
            count = episodeObservableCollection.Count();

            if (previousEpIndex < count)
            {
                previousEpisode = episodeObservableCollection.ElementAt(previousEpIndex);
                previousButton.IsEnabled = true;
            }
            else
            {
                previousEpisode = null;
                previousButton.IsEnabled = false;
            }
            if (nextEpIndex >= 0)
            {
                nextEpisode = episodeObservableCollection.ElementAt(nextEpIndex);
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
            DabUserInteractionEvents.WaitStopped(source, new EventArgs());
        }

        public void OnMonthSelected(object o, EventArgs e)
        {
            TimedActions();
        }

        async void OnChannel(object o, EventArgs e)
        /* User selected a different channel */
        {
            //Wait indicator 
            DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Loading channel...", true));


            List<string> emptyList = new List<string>();

            //Load the episode list
            if (CrossConnectivity.Current.IsConnected || PlayerFeedAPI.GetEpisodeList((Resource)ChannelsList.SelectedItem).Count() > 0)
            {
                //Store the resource / channel
                _resource = (Resource)ChannelsList.SelectedItem;
                BackgroundImage.Source = _resource.images.backgroundTablet;



                //get the episodes - this routine handles resetting the date and raising events
                DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Refreshing episodes...", true));
                var result = await DabServiceRoutines.GetEpisodes(_resource.id);

                //Load the list if episodes for the channel.
                Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
                TimedActions();

                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
            }
            else
            {
                //No episodes available
                await DisplayAlert("Unable to get episodes for channel.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
            }

            DabUserInteractionEvents.WaitStopped(source, new EventArgs());
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
                DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
                var newEp = previousEpisode;
                currentIndex = currentIndex + 1;
                previousEpIndex = currentIndex + 1;
                nextEpIndex = currentIndex - 1;
                count = episodeObservableCollection.Count();

                if (previousEpIndex < count)
                {
                    previousEpisode = episodeObservableCollection.ElementAt(previousEpIndex);
                    previousButton.IsEnabled = true;
                }
                else
                {
                    previousEpisode = null;
                    previousButton.IsEnabled = false;
                }
                if (nextEpIndex >= 0)
                {
                    nextEpisode = episodeObservableCollection.ElementAt(nextEpIndex);
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
                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
                player.Pause();
            }
            catch (Exception ex)
            {
                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
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
                DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
                var newEp = nextEpisode;
                currentIndex = currentIndex - 1;
                previousEpIndex = currentIndex + 1;
                nextEpIndex = currentIndex - 1;
                count = episodeObservableCollection.Count();


                if (previousEpIndex < count)
                {
                    previousEpisode = episodeObservableCollection.ElementAt(previousEpIndex);
                    previousButton.IsEnabled = true;
                }
                else
                {
                    previousEpisode = null;
                    previousButton.IsEnabled = false;
                }
                if (nextEpIndex >= 0)
                {
                    nextEpisode = episodeObservableCollection.ElementAt(nextEpIndex);
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
                DabUserInteractionEvents.WaitStopped(o, new EventArgs());
                player.Pause();
            }
            catch (Exception ex)
            {
                DabUserInteractionEvents.WaitStopped(o, new EventArgs());
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
            player.IsReady = true;
        }

        //Share the episode
        void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(episode.Episode.channel_code, episode.Episode.PubDate.ToString("MMddyyyy"));
        }

        //User login
        void OnLogin(object o, EventArgs e)
        {
            GlobalResources.LogoffAndResetApp();
        }

        #endregion

        #region eventhandlers 

        async void OnJournalChanged(object o, EventArgs e)
        {
            if (JournalContent.IsFocused)
            {
                journal.UpdateJournal(episode.Episode.PubDate, JournalContent.Text);
                if (JournalContent.Text.Length == 0)
                {
                    episode.Episode.UserData.HasJournal = false;
                    episode.HasJournal = false;

                    await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, false, null);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, DabService.ServiceActionsEnum.Journaled, null, null, null, true);
                }
                else if (episode.Episode.UserData.HasJournal == false && JournalContent.Text.Length > 0)
                {
                    episode.Episode.UserData.HasJournal = true;
                    episode.HasJournal = true;

                    await PlayerFeedAPI.UpdateEpisodeUserData((int)episode.Episode.id, episode.IsListenedTo, episode.IsFavorite, true, null);
                    await AuthenticationAPI.CreateNewActionLog((int)episode.Episode.id, DabService.ServiceActionsEnum.Journaled, null, null, null, null);
                }
            }

        }

        void OnDisconnect(object o, EventArgs e)
        {
            Debug.WriteLine($"Disoconnected from journal server: {o.ToString()}");
            JournalWarning.IsEnabled = true;
        }

        async void OnReconnect(object o, EventArgs e)
        {
            DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Reconnecting to the journal service...", true));

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
            DabUserInteractionEvents.WaitStopped(o, new EventArgs());
        }


        //Journal was edited
        void OnEdit(object o, EventArgs e)
        {
            journal.ExternalUpdate = false;
            //JournalTracker.Current.socket.ExternalUpdate = false;
        }

        //Journal editing finished?
        void OffEdit(object o, EventArgs e)
        {
            journal.ExternalUpdate = true;
            if (!journal.IsConnected)
            {
                journal.Reconnect();
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

        async void OnFilters(object o, EventArgs e)
        {
            var popup = new DabPopupEpisodeMenu(_resource);
            popup.ChangedRequested += Popup_ChangedRequested;
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(popup, false);
        }

        private async void Popup_ChangedRequested(object sender, EventArgs e)
        {
            var popuPage = (DabPopupEpisodeMenu)sender;
            _resource = popuPage.Resource;
            await Refresh(EpisodeRefreshType.NoRefresh);
        }

        #endregion
    }
}
