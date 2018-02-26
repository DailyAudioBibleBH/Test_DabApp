﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.Connectivity;
using SlideOverKit;
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
            Offline.IsToggled = resource.availableOffline;
            var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
            foreach (var month in months)
            {
                Months.Items.Add(month);
            }
            Months.Items.Add("My Journals");
            Months.Items.Add("My Favorites");
            Months.SelectedIndex = 0;
            EpisodeList.RefreshCommand = new Command(async () => { await Refresh(); EpisodeList.IsRefreshing = false; });
            Device.StartTimer(TimeSpan.FromMinutes(5), () =>
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
            if (chosen.is_downloaded || CrossConnectivity.Current.IsConnected)
            {
                if (chosen.id != AudioPlayer.Instance.CurrentEpisodeId)
                {
                    JournalTracker.Current.Content = null;
                }
                await Navigation.PushAsync(new DabPlayerPage(chosen, _reading));
            }
            else await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played offline download them before going offline.", "OK");
            EpisodeList.SelectedItem = null;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
            EpisodeList.IsEnabled = true;
        }

        public void OnOffline(object o, ToggledEventArgs e) {
            _resource.availableOffline = e.Value;
            ContentAPI.UpdateOffline(e.Value, _resource.id);
            if (e.Value)
            {
                Task.Run(async () => { await PlayerFeedAPI.DownloadEpisodes(); });
            }
            else {
                Task.Run(async () => {
                    await PlayerFeedAPI.DeleteChannelEpisodes(_resource);
                    Device.BeginInvokeOnMainThread(() => { TimedActions(); });
                });
            }
        }

        public void OnMonthSelected(object o, EventArgs e) {
            TimedActions();
        }

        public async void OnListened(object o, EventArgs e)
        {
            var mi = ((MenuItem)o);
            var ep = ((EpisodeViewModel)mi.CommandParameter).Episode;
            if (ep.is_listened_to == "listened")
            {
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, "");
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "");
            }
            else
            {
                await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id);
                await AuthenticationAPI.CreateNewActionLog((int)ep.id, "listened", ep.stop_time, "listened");
            }
            TimedActions();
        }

        public async void OnFavorite(object o, EventArgs e)
        {
            var mi = ((MenuItem)o);
            var ep = ((EpisodeViewModel)mi.CommandParameter).Episode;
            await PlayerFeedAPI.UpdateEpisodeProperty((int)ep.id, "is_favorite");
            await AuthenticationAPI.CreateNewActionLog((int)ep.id, "favorite", ep.stop_time, null, !ep.is_favorite);
            TimedActions();
        }

        async Task Refresh()
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            await PlayerFeedAPI.GetEpisodes(_resource);
            await AuthenticationAPI.GetMemberData();
            TimedActions();
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            TimedActions();
        }

        void TimedActions()
        {
            //Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
            if ((string)Months.SelectedItem == "My Favorites")
            {
                EpisodeList.ItemsSource = _Episodes = Episodes.Where(x => x.is_favorite == true).Select(e => new EpisodeViewModel(e)).ToList();
                Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
            }
            else
            {
                if ((string)Months.SelectedItem == "My Journals")
                {
                    EpisodeList.ItemsSource = _Episodes = Episodes.Where(x => x.has_journal == true).Select(x => new EpisodeViewModel(x)).ToList();
                    //EpisodeList.ItemsSource = list;
                    Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
                }
                else
                {
                    EpisodeList.ItemsSource = _Episodes = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]).Select(x => new EpisodeViewModel(x)).ToList();
                    //EpisodeList.ItemsSource = list;
                    Container.HeightRequest = EpisodeList.RowHeight * _Episodes.Count();
                }
            }
        }
    }
}
