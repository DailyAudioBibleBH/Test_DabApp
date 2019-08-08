using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;
using FFImageLoading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DABApp
{
    public partial class DabChannelsPage : DabBaseContentPage
    {
        View ChannelView;
        dbEpisodes episode;
        Resource _resource;
        bool IsUnInitialized = true;
        private double _width;
        private double _height;
        private int number;

        public DabChannelsPage()
        {
            InitializeComponent();


            ////Choose a different control template to disable built in scroll view
            //ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
            //this.ControlTemplate = playerBarTemplate;

            //Init the form
            DabViewHelper.InitDabForm(this);
            ChannelView = ContentConfig.Instance.views.Single(x => x.id == 56); //TODO: Find this using a key vs. a specific number
            BindingContext = ChannelView;
            _resource = ChannelView.resources[0];

            //Calculate view sizes / scrolling information
            _width = Width;
            _height = Height;
            var remainder = ChannelView.resources.Count() % GlobalResources.Instance.FlowListViewColumns;
            number = ChannelView.resources.Count() / GlobalResources.Instance.FlowListViewColumns;
            if (remainder != 0)
            {
                number += 1;
            }
            if (GlobalResources.Instance.ThumbnailImageHeight != 0)
            {
                ChannelsList.HeightRequest = Device.Idiom == TargetIdiom.Tablet ? number * (GlobalResources.Instance.ThumbnailImageHeight + 60) + 120 : number * (GlobalResources.Instance.ThumbnailImageHeight + 60);
            }
            else ChannelsList.HeightRequest = GlobalResources.Instance.ScreenSize > 1000 ? 1500 : 1000;

            /* SET UP TIMERS */

            Device.StartTimer(TimeSpan.FromMinutes(5), () =>
            {
                TimedActions();
                return true;
            });

            Device.StartTimer(TimeSpan.FromMinutes(5), () =>
            {
                if (!JournalTracker.Current.IsConnected)
                {
                    ConnectJournal();
                }
                return true;
            });
        }

        void PostLogs()
        {
            Task.Run(async () =>
            {
                await AuthenticationAPI.PostActionLogs();
            });
        }

        async void OnPlayer(object o, EventArgs e)
        {
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            var reading = await PlayerFeedAPI.GetReading(episode.read_link);
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                await PlayerFeedAPI.GetEpisodes(_resource); //Get episodes prior to pushing up the TabletPage
                await Navigation.PushAsync(new DabTabletPage(_resource));
            }
            else
            {
                await Navigation.PushAsync(new DabPlayerPage(episode, reading));
            }
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            HideMenu();
        }

        //Navigate to a specific channel
        async void OnChannel(object o, ItemTappedEventArgs e)
        {

            //Wait indicator
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;

            //Selected resource
            var selected = (Resource)e.Item;
            selected.IsNotSelected = .5;
            var resource = (Resource)e.Item;
            var episodes = await PlayerFeedAPI.GetEpisodes(resource); //Get episodes before pushing to the episodes page.
            if (!episodes.Contains("error") || PlayerFeedAPI.GetEpisodeList(resource).Count() > 0)
            {
                //Navigate to the appropriate player page 
                if (Device.Idiom == TargetIdiom.Tablet)
                {
                    await Navigation.PushAsync(new DabTabletPage(resource));
                }
                else
                {
                    await Navigation.PushAsync(new DabEpisodesPage(resource));
                }
            }
            else await DisplayAlert("Unable to get episodes for Channel.", "This may be due to problems with your internet connection.  Please check your internet connection and try again.", "OK");
            selected.IsNotSelected = 1.0;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;

            //Send info to Firebase analytics that user accessed a channel
            var infoJ = new Dictionary<string, string>();
            infoJ.Add("channel", resource.title);
            DependencyService.Get<IAnalyticsService>().LogEvent("player_channel_selected", infoJ);

            //TODO: Subscribe to a channel

        }

        void TimedActions()
        {
            if (!AuthenticationAPI.CheckToken(0))
            {
                Task.Run(async () =>
                {
                        //Try to exchange token for a fresh one
                        await AuthenticationAPI.ExchangeToken();
                });
            }
            //Clean up old episodes
            PlayerFeedAPI.CleanUpEpisodes();
            Task.Run(async () =>
            {
                //Download new episodes
                await PlayerFeedAPI.DownloadEpisodes();
            });
            if (GlobalResources.GetUserName() != "Guest Guest")
            {
                Task.Run(async () =>
                {
                    //Send data to the server
                    await AuthenticationAPI.PostActionLogs();
                    await AuthenticationAPI.GetMemberData();
                });
            }
        }

        void ConnectJournal()
        {
            //Reconnect the journal
            AuthenticationAPI.ConnectJournal();
        }

        protected override async void OnAppearing()
        {
            MessagingCenter.Send<string>("Setup", "Setup");
            foreach (var r in ChannelView.resources)
            {
                r.AscendingSort = false;
                r.filter = EpisodeFilters.None;
            }
            base.OnAppearing();
            if (ChannelsList.HeightRequest == 1000 && GlobalResources.Instance.ThumbnailImageHeight != 0)
            {
                ChannelsList.HeightRequest = Device.Idiom == TargetIdiom.Tablet ? number * (GlobalResources.Instance.ThumbnailImageHeight + 60) + 120 : number * (GlobalResources.Instance.ThumbnailImageHeight + 60);
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            //Deal with resizing (tablets)
            double oldwidth = _width;
            base.OnSizeAllocated(width, height);
            if (Equals(_width, width) && Equals(_height, height)) return;
            _width = width;
            _height = height;
            if (Equals(oldwidth, -1)) return;
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                GlobalResources.Instance.FlowListViewColumns = width > height ? 4 : 3;
            }
            GlobalResources.Instance.ThumbnailImageHeight = (App.Current.MainPage.Width / GlobalResources.Instance.FlowListViewColumns) - 30;
        }
    }
}
