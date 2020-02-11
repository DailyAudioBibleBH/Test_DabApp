using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            _resource = resource;
            DabViewHelper.InitDabForm(this);
            Episodes = PlayerFeedAPI.GetEpisodeList(resource);
            //EpisodeList.ItemsSource = Episodes;
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
                Task.Run(async () => {
                    await PlayerFeedAPI.DownloadEpisodes();
                    CircularProgressControl circularProgressControl = ControlTemplateAccess.FindTemplateElementByName<CircularProgressControl>(this, "circularProgressControl");
                    circularProgressControl.HandleDownloadVisibleChanged(true);
                });
            }
            EpisodeList.RefreshCommand = new Command(async () => { await Refresh(); EpisodeList.IsRefreshing = false; });
            MessagingCenter.Subscribe<string>("Update", "Update", (obj) => {
                TimedActions();
            });

            //Subscribe to GraphQL alerts for refresh
            MessagingCenter.Subscribe<string>("dabapp", "EpisodeDataChanged", (obj) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    TimedActions();
                });

            });

            Device.StartTimer(TimeSpan.FromSeconds(15), () =>
            {
                TimedActions();
                return true;
            });

            
        }       

        public async void OnEpisode(object o, ItemTappedEventArgs e)
        {
            EpisodeList.IsEnabled = false;
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
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
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
            EpisodeList.IsEnabled = true;

            MessagingCenter.Subscribe<string>("Update", "Update", (obj) =>
            {
                TimedActions();
            });
        }

        public void OnMonthSelected(object o, EventArgs e) {
            TimedActions();
        }

        public async void OnListened(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, !ep.is_listened_to, null, null, null, false);
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", null, !ep.is_listened_to);
            model.listenedToVisible = !ep.is_listened_to;
        }

        public async void OnFavorite(object o, EventArgs e)
        {
            var mi = ((Xamarin.Forms.MenuItem)o);
            var model = ((EpisodeViewModel)mi.CommandParameter);
            var ep = model.Episode;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, null, !ep.is_favorite, null, null, false);
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, "favorite", null, null, !ep.is_favorite);
            model.favoriteVisible = !ep.is_favorite;
        }

        

        async Task Refresh()
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;

            await AuthenticationAPI.PostActionLogs();
            //await PlayerFeedAPI.GetEpisodes(_resource);
            await AuthenticationAPI.GetMemberData();
            TimedActions();

            GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();

            if (_resource.availableOffline)
            {
                Task.Run(async () => {
                    await PlayerFeedAPI.DownloadEpisodes();
                    CircularProgressControl circularProgressControl = ControlTemplateAccess.FindTemplateElementByName<CircularProgressControl>(this, "circularProgressControl");
                    circularProgressControl.HandleDownloadVisibleChanged(true);
                });
            }
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
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
            EpisodeList.ItemsSource = _Episodes = Episodes
                .Where(x => Months.Items[Months.SelectedIndex] == "All Episodes" ? true : x.PubMonth == Months.Items[Months.SelectedIndex].Substring(0, 3))
                .Where(x => _resource.filter == EpisodeFilters.Favorite ? x.is_favorite : true)
                .Where(x => _resource.filter == EpisodeFilters.Journal ? x.has_journal : true)
                .Select(x => new EpisodeViewModel(x)).ToList();
            Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
        }
    }
}
