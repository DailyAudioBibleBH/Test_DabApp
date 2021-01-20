using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using Newtonsoft.Json;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
    enum EpisodeRefreshType //enum specifying white type of episode refresh to perform
    {
        FullRefresh, //load all episodes we can back to min date
        IncrementalRefresh, //load all episodes back to last date
        NoRefresh //simply refresh / filter data, not loading of new episodes
    }

    public partial class DabEpisodesPage : DabBaseContentPage
    {
        #region constructor and startup methods

        Resource _resource;
        IEnumerable<dbEpisodes> Episodes;
        List<EpisodeViewModel> _Episodes;
        object source;
        bool Initializing;

        public DabEpisodesPage(Resource resource)
        {
            Initializing = true;
            InitializeComponent();
            DabViewHelper.InitDabForm(this);

            //UI setup
            _resource = resource; //the resource (channel) being used
            BindingContext = this;
            bannerImage.Source = resource.images.bannerPhone;
            bannerContent.Text = resource.title;

            //For Wait Start and Stop
            source = new object();

            //pull to refresh
            EpisodeList.RefreshCommand = new Command(async () => //pull to refresh command
            {
                await Refresh(EpisodeRefreshType.FullRefresh);
                EpisodeList.IsRefreshing = false;
            });

            //initially bind to episodes we have before trying to reload on appearing
            Refresh(EpisodeRefreshType.NoRefresh); //refresh episode list

            //Subscribe to looking for new episodes when user returns to app
            MessagingCenter.Subscribe<string>("DabApp", "OnResume", (obj) =>
            {
                //get new episodes, if they exist -- this will also handle downloading
                Task task = Refresh(EpisodeRefreshType.IncrementalRefresh); //refresh episode list
            });

            Initializing = false;
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            //episodes changed event
            DabServiceEvents.EpisodesChangedEvent -= DabServiceEvents_EpisodesChangedEvent;

            //episode user data changed event
            DabServiceEvents.EpisodeUserDataChangedEvent -= DabServiceEvents_EpisodeUserDataChangedEvent;

        }

        protected async override void OnAppearing()
        {
            /*
             * page appearing
             * when page appears, get new episodes, and then download them, if needed
             */

            base.OnAppearing();

            //episodes changed event
            DabServiceEvents.EpisodesChangedEvent += DabServiceEvents_EpisodesChangedEvent;

            //episode user data changed event
            DabServiceEvents.EpisodeUserDataChangedEvent += DabServiceEvents_EpisodeUserDataChangedEvent;

            //get new episodes, if they exist -- this will also handle downloading
            await Refresh(EpisodeRefreshType.IncrementalRefresh); //refresh episode list
        }

        #endregion

        #region refresh and download processing

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

            if (refreshType != EpisodeRefreshType.NoRefresh)
            {
                //refresh episodes from the server
                //get the episodes - this routine handles resetting the date and raising events
                DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Refreshing episodes...", true));
                var result = await DabServiceRoutines.GetEpisodes(_resource.id, (refreshType == EpisodeRefreshType.FullRefresh));
                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
            }

            //get the rull list of episodes for the resource
            Episodes = PlayerFeedAPI.GetEpisodeList(_resource);

            //Update year list
            if (Years.Items.Contains("All Episodes") == false)
            {
                Years.Items.Insert(0, "All Episodes"); //default selector
                Years.SelectedIndex = 0;

            }
            var years = Episodes.Select(x => x.PubYear).Distinct().ToList();
            foreach (var year in years)
            {
                if (Years.Items.Contains(year.ToString()) == false)
                {
                    Years.Items.Add(year.ToString());
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
                    List<EpisodeViewModel> EpisodesList = _Episodes = Episodes
                        .Where(x => Years.Items[Years.SelectedIndex] == "All Episodes" ? true : x.PubYear.ToString() == Years.Items[Years.SelectedIndex])
                        .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.UserData.IsFavorite : true)
                        .Where(x => _resource.filter == EpisodeFilters.Journal ? x.UserData.HasJournal : true)
                        .Select(x => new EpisodeViewModel(x)).ToList();

                    foreach (var item in EpisodesList)
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
                        if (EpisodesList.IndexOf(item) >= 20)
                        {
                            break;
                        }
                    }

                    EpisodeList.ItemsSource = EpisodesList;

                    Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
                }
                );
            }

            //download any new episodes
            await DownloadEpisodes();

        }

        async Task<bool> DownloadEpisodes()
        {
            /*
             * download episodes 
             */
            if (_resource.availableOffline)
            {
                await PlayerFeedAPI.DownloadEpisodes();
                //CircularProgressControl circularProgressControl = ControlTemplateAccess.FindTemplateElementByName<CircularProgressControl>(this, "circularProgressControl");
                //circularProgressControl?.HandleDownloadVisibleChanged(true);
            }

            return true;
        }

        private async void DabServiceEvents_EpisodesChangedEvent()
        {
            //new episodes added - refresh the list
            await Refresh(EpisodeRefreshType.IncrementalRefresh);
        }

        private async void DabServiceEvents_EpisodeUserDataChangedEvent()
        {
            //user data has changed (not episode list itself)
            await Refresh(EpisodeRefreshType.NoRefresh);
        }

        #endregion

        #region user interaction methods

        public async void OnRefresh(object o, EventArgs e)
        {
            /*
             * handles the click of the refresh button 
             */
            btnRefresh.RotateTo(360, 2000).ContinueWith(x => btnRefresh.RotateTo(0, 0)); ; //don't await this as we want to get started with the code right away
            await Refresh(EpisodeRefreshType.FullRefresh);
        }

        public async void OnEpisode(object o, ItemTappedEventArgs e)
        {
            /*
             * click on an episode to play it
             */

            EpisodeList.IsEnabled = false; //disable the list while we work with it.
            DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
            var chosenVM = (EpisodeViewModel)e.Item;
            var chosen = chosenVM.Episode;
            EpisodeList.SelectedItem = null;

            if (chosen.File_name_local != null || CrossConnectivity.Current.IsConnected)
            {
                //Push the new player page
                await Navigation.PushAsync(new DabPlayerPage(chosen));
            }
            else
            {
                //let user know you can't play an episode if not downloaded and offline
                await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played offline download them before going offline.", "OK");
            }
            EpisodeList.SelectedItem = null; //deselect the item
            DabUserInteractionEvents.WaitStopped(source, new EventArgs());
            EpisodeList.IsEnabled = true;
        }

        public async void OnYearSelected(object o, EventArgs e)
        {
            //filter to a given month
            //dont run refresh first time round, already gets hit
            if (!Initializing)
            {
                await Refresh(EpisodeRefreshType.NoRefresh);
            }
        }

        public async void OnListened(object o, EventArgs e)
        {
            /*
             * handle listened of an episode via the list
             */
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                var mi = ((Xamarin.Forms.MenuItem)o);
                var model = ((EpisodeViewModel)mi.CommandParameter);
                var ep = model.Episode;
                var newValue = !ep.UserData.IsListenedTo;
                model.IsListenedTo = newValue;
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, DabService.ServiceActionsEnum.Listened, null, newValue);
            }
            else
            {
                //guest mode - do nothing
                await DisplayAlert("Guest Mode", "You are currently logged in as a guest. Please log in to use this feature", "OK");
            }
        }

        public async void OnFavorite(object o, EventArgs e)
        {
            /*
             * handle favorite of an episode via the list
             */
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                var mi = ((Xamarin.Forms.MenuItem)o);
                var model = ((EpisodeViewModel)mi.CommandParameter);
                var ep = model.Episode;
                var newValue = !ep.UserData.IsFavorite;
                model.IsFavorite = newValue;
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, DabService.ServiceActionsEnum.Favorite, null, null, newValue);
            }
            else
            {
                //guest mode - do nothing
                await DisplayAlert("Guest Mode", "You are currently logged in as a guest. Please log in to use this feature", "OK");
            }
        }

        void OnFilters(object o, EventArgs e)
        {
            /*
             * show the filter list
             */
            var popup = new DabPopupEpisodeMenu(_resource);
            popup.ChangedRequested += Popup_ChangedRequested;
            Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(popup);
        }

        private async void Popup_ChangedRequested(object sender, EventArgs e)
        {
            /* 
             * handle changes to the filter list
             */
            var popuPage = (DabPopupEpisodeMenu)sender;
            _resource = popuPage.Resource;
            await Refresh(EpisodeRefreshType.NoRefresh);
        }

        #endregion

    }
}