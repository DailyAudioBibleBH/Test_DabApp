using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.Service;
using Newtonsoft.Json;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
    enum EpisodeRefreshType
    {
        FullRefresh,
        IncrementalRefresh,
        NoRefresh
    }


    public partial class DabEpisodesPage : DabBaseContentPage
    {
        Resource _resource;
        IEnumerable<dbEpisodes> Episodes;
        List<EpisodeViewModel> _Episodes;

        public DabEpisodesPage(Resource resource)
        {
            InitializeComponent();
            DabViewHelper.InitDabForm(this);

            //UI setup
            _resource = resource; //the resource (channel) being used
            BindingContext = this;
            bannerImage.Source = resource.images.bannerPhone;
            bannerContent.Text = resource.title;

            //pull to refresh
            EpisodeList.RefreshCommand = new Command(async () => //pull to refresh command
            {
                await Refresh(EpisodeRefreshType.FullRefresh);
                EpisodeList.IsRefreshing = false;
            });

            //episodes added event
            DabServiceEvents.EpisodesChangedEvent += DabServiceEvents_EpisodesChangedEvent;

        }

        private async void DabServiceEvents_EpisodesChangedEvent()
        {
            //new episodes added - refresh the list
            await Refresh(EpisodeRefreshType.IncrementalRefresh);
        }

        async Task<bool> DownloadEpisodes()
        {
            /*
             * download episodes 
             */
            if (_resource.availableOffline)
            {
                await PlayerFeedAPI.DownloadEpisodes();
                CircularProgressControl circularProgressControl = ControlTemplateAccess.FindTemplateElementByName<CircularProgressControl>(this, "circularProgressControl");
                circularProgressControl.HandleDownloadVisibleChanged(true);
            }

            return true;
        }

        protected async override void OnAppearing()
        {
            /*
             * page appearing
             * when page appears, get new episodes, and then download them, if needed
             */

            base.OnAppearing();

            //get new episodes, if they exist -- this will also handle downloading
            Refresh(EpisodeRefreshType.NoRefresh); //refresh list with no new data first
            Refresh(EpisodeRefreshType.IncrementalRefresh); //run it again looking for new data

        }

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

            EpisodeList.IsEnabled = false;
            GlobalResources.WaitStart();
            var chosenVM = (EpisodeViewModel)e.Item;
            var chosen = chosenVM.Episode;
            EpisodeList.SelectedItem = null;
            var _reading = await PlayerFeedAPI.GetReading(chosen.read_link);

            if (chosen.File_name_local != null || CrossConnectivity.Current.IsConnected)
            {
                //play the local file or stream it via the internet

                if (chosen.id != GlobalResources.CurrentEpisodeId)
                {
                    //TODO: Replace this with sync
                    //JournalTracker.Current.Content = null;
                }
                //Push the new player page
                await Navigation.PushAsync(new DabPlayerPage(chosen, _reading));
            }
            else
            {
                //let user know you can't play an episode if not downloaded and offline
                await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played offline download them before going offline.", "OK");
            }
            EpisodeList.SelectedItem = null;
            GlobalResources.WaitStop();
            EpisodeList.IsEnabled = true;
        }

        public async void OnMonthSelected(object o, EventArgs e)
        {
            //filter to a given month
            await Refresh(EpisodeRefreshType.NoRefresh);
        }

        public async void OnListened(object o, EventArgs e)
        {
            /*
             * handle listened of an episode via the list
             */

            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, DabService.ServiceActionsEnum.Listened, null, !ep.UserData.IsListenedTo);
            model.IsListenedTo = !ep.UserData.IsListenedTo;
        }

        public async void OnFavorite(object o, EventArgs e)
        {
            /*
             * handle favorite of an episode via the list
             */

            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, DabService.ServiceActionsEnum.Favorite, null, null, !ep.UserData.IsFavorite);
            model.IsFavorite = !ep.UserData.IsFavorite;
        }



        async Task Refresh(EpisodeRefreshType refreshType)
        {
            /* 
             * this routine pulls any new episodes for the selected channel, 
             * updates the ui, 
             * and downloads them
             * 
             */

            DateTime lastRefreshDate = Convert.ToDateTime(GlobalResources.GetLastRefreshDate(_resource.id));
            DateTime minQueryDate;

            if (refreshType != EpisodeRefreshType.NoRefresh)
            {
                //refresh episodes

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
                GlobalResources.WaitStart($"Refreshing episodes...");
                var result = await DabServiceRoutines.GetEpisodes(_resource.id, (refreshType == EpisodeRefreshType.FullRefresh));
                GlobalResources.WaitStop();
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
                if (Months.Items.Contains(month) == false)
                { 
                    Months.Items.Add(month);
                }
            }

            //update the UI

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
                    EpisodeList.ItemsSource = _Episodes = Episodes
                        .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Months.Items[Months.SelectedIndex])
                        .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.UserData.IsFavorite : true)
                        .Where(x => _resource.filter == EpisodeFilters.Journal ? x.UserData.HasJournal : true)
                        .Select(x => new EpisodeViewModel(x)).ToList();

                    Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
                }
                );
            }

            //download any new episodes
            await DownloadEpisodes();

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


    }
}
