using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using Newtonsoft.Json;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabEpisodesPage : DabBaseContentPage
    {
        Resource _resource;
        IEnumerable<dbEpisodes> Episodes;
        List<EpisodeViewModel> _Episodes;

        public DabEpisodesPage(Resource resource)
        {
            InitializeComponent();
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");

            //Subscribe to GraphQL alerts for refresh
            MessagingCenter.Subscribe<string>("dabapp", "EpisodeDataChanged", (obj) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    TimedActions();
                });

            });

            _resource = resource;
            DabViewHelper.InitDabForm(this);
            Episodes = PlayerFeedAPI.GetEpisodeList(resource);
            BindingContext = this;
            bannerImage.Source = resource.images.bannerPhone;
            bannerContent.Text = resource.title;
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                var m = MonthConverter.ConvertToFull(month);
                Months.Items.Add(m);
            }
            Months.Items.Insert(0, "All Episodes");
            Months.SelectedIndex = 0;
            if (resource.availableOffline)
            {
                Task.Run(async () =>
                {
                    await PlayerFeedAPI.DownloadEpisodes();
                    CircularProgressControl circularProgressControl = ControlTemplateAccess.FindTemplateElementByName<CircularProgressControl>(this, "circularProgressControl");
                    circularProgressControl.HandleDownloadVisibleChanged(true);
                });
            }
            EpisodeList.RefreshCommand = new Command(async () => { await Refresh(true); EpisodeList.IsRefreshing = false; });
            MessagingCenter.Subscribe<string>("Update", "Update", (obj) =>
            {
                Episodes = PlayerFeedAPI.GetEpisodeList(resource);
                TimedActions();
            });

            MessagingCenter.Subscribe<string>("DabApp", "OnResume", (obj) => {
                Refresh(false);
                }
            ); //app activated

            MessagingCenter.Subscribe<string>("dabapp", "OnEpisodesUpdated", (obj) => {
                Refresh(false);
            }
            ); //app activated
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Refresh(false);
        }

        public async void OnEpisode(object o, ItemTappedEventArgs e)
        {
            try
            {
                EpisodeList.IsEnabled = false;
                GlobalResources.WaitStart();
                var chosenVM = (EpisodeViewModel)e.Item;
                var chosen = chosenVM.Episode;
                EpisodeList.SelectedItem = null;
                var _reading = await PlayerFeedAPI.GetReading(chosen.read_link);

                if (chosen.File_name_local != null || CrossConnectivity.Current.IsConnected)
                {
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
                    await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played offline download them before going offline.", "OK");
                }
                EpisodeList.SelectedItem = null;
                GlobalResources.WaitStop();
                EpisodeList.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Refresh(true);
            }
        }

        public void OnMonthSelected(object o, EventArgs e)
        {
            TimedActions();
        }

        public async void OnListened(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, !ep.UserData.IsListenedTo, null, null, null, false);
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", null, !ep.UserData.IsListenedTo);
            model.IsListenedTo = !ep.UserData.IsListenedTo;
        }

        public async void OnFavorite(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, null, !ep.UserData.IsFavorite, null, null, false);
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, "favorite", null, null, !ep.UserData.IsFavorite);
            model.IsFavorite = !ep.UserData.IsFavorite;
        }



        async Task Refresh(bool ReloadAll)
        {
            DateTime lastRefreshDate = Convert.ToDateTime(GlobalResources.GetLastRefreshDate(_resource.id));
            int pullToRefreshRate = GlobalResources.PullToRefreshRate;
            string minQueryDate;
            if (ReloadAll)
            {
                if (DateTime.Now.Subtract(lastRefreshDate).TotalMinutes >= pullToRefreshRate)
                {
                    minQueryDate = DateTime.MinValue.ToUniversalTime().ToString("o");
                    GlobalResources.SetLastRefreshDate(_resource.id);
                }
                else
                {
                    return; //don't do anything if they've recently pulled to refresh
                }
            }
            else
            {
                minQueryDate = GlobalResources.GetLastEpisodeQueryDate(_resource.id);
            }

            GlobalResources.WaitStart($"Refreshing episodes...");

            //send websocket message to get episodes by channel
            DabGraphQlVariables variables = new DabGraphQlVariables();
            Debug.WriteLine($"Getting episodes by ChannelId");
            var episodesByChannelQuery = "query { episodes(date: \"" + minQueryDate + "\", channelId: " + _resource.id + ") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
            var episodesByChannelPayload = new DabGraphQlPayload(episodesByChannelQuery, variables);
            string JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", episodesByChannelPayload));
            DabSyncService.Instance.Send(JsonIn);

            if (GlobalResources.GetUserEmail() != "Guest")
            {
                await AuthenticationAPI.PostActionLogs(false);
                //await PlayerFeedAPI.GetEpisodes(_resource);
                await AuthenticationAPI.GetMemberData();
                TimedActions();

                GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();

                if (_resource.availableOffline)
                {
                    Task.Run(async () =>
                    {
                        await PlayerFeedAPI.DownloadEpisodes();
                        CircularProgressControl circularProgressControl = ControlTemplateAccess.FindTemplateElementByName<CircularProgressControl>(this, "circularProgressControl");
                        circularProgressControl.HandleDownloadVisibleChanged(true);
                    });
                }
            }


            GlobalResources.WaitStop();
        }

        void OnFilters(object o, EventArgs e)
        {
            var popup = new DabPopupEpisodeMenu(_resource);
            popup.ChangedRequested += Popup_ChangedRequested;
            Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(popup);
        }

        private void Popup_ChangedRequested(object sender, EventArgs e)
        {
            var popuPage = (DabPopupEpisodeMenu)sender;
            _resource = popuPage.Resource;
            TimedActions();
        }

        public void TimedActions()
        {
            if (_resource.AscendingSort)
            {
                Episodes = Episodes.OrderBy(x => x.PubDate);
            }
            else
            {
                Episodes = Episodes.OrderByDescending(x => x.PubDate);
            }
            if (Episodes.Count() > 0)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    EpisodeList.ItemsSource = _Episodes = Episodes
                    .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Months.Items[Months.SelectedIndex].Substring(0, 3))
                    .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.UserData.IsFavorite : true)
                    .Where(x => _resource.filter == EpisodeFilters.Journal ? x.UserData.HasJournal : true)
                    .Select(x => new EpisodeViewModel(x)).ToList();
                    Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
                }
                );
            }
        }
    }
}
