using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;
using FFImageLoading;
using System.Threading.Tasks;
using System.Diagnostics;
using DABApp.DabSockets;
using Newtonsoft.Json;
using SQLite;
using Rg.Plugins.Popup.Services;
using DABApp.DabUI;
using Plugin.Connectivity;

namespace DABApp
{
    public partial class DabChannelsPage : DabBaseContentPage
    {
        View ChannelView;
        dbEpisodes episode;
        Resource _resource;
        private double _width;
        private double _height;
        private int number;
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        private bool todaysEpisodeVisible = false;

        public DabChannelsPage()
        {
            InitializeComponent();
            //Take away back button on navbar
            NavigationPage.SetHasBackButton(this, false);
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

            //Connect to the SyncService
            DabSyncService.Instance.Init();
            DabSyncService.Instance.Connect();

            /* SET UP TIMERS (run once initially)*/
            TimedActions();
            Device.StartTimer(TimeSpan.FromMinutes(5), () =>
            {
                TimedActions();
                return true;
            });
        }

        void PostLogs()
        {
            Task.Run(async () =>
            {
                await AuthenticationAPI.PostActionLogs(false);
            });
        }

        async void OnPlayer(object o, EventArgs e)
        {
            GlobalResources.WaitStart();
            var reading = await PlayerFeedAPI.GetReading(episode.read_link);
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                //await PlayerFeedAPI.GetEpisodes(_resource); //Get episodes prior to pushing up the TabletPage
                await Navigation.PushAsync(new DabTabletPage(_resource));
            }
            else
            {
                await Navigation.PushAsync(new DabPlayerPage(episode, reading));
            }
            GlobalResources.WaitStop();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            HideMenu();
        }

        //Navigate to a specific channel
        async void OnChannel(object o, ItemTappedEventArgs e)
        {
            try
            {
                GlobalResources.WaitStart();

                //Selected resource
                var selected = (Resource)e.Item;
                selected.IsNotSelected = .5;
                var resource = (Resource)e.Item;

                if (DabSyncService.Instance.IsDisconnected)
                {
                    DabSyncService.Instance.Connect();
                }

                //Removed this because the player page will request it on it's on in OnAppearing
                ////send websocket message to get episodes by channel
                //string lastEpisodeQueryDate = GlobalResources.GetLastEpisodeQueryDate(resource.id);
                //DabGraphQlVariables variables = new DabGraphQlVariables();
                //Debug.WriteLine($"Getting episodes by ChannelId");
                //var episodesByChannelQuery = "query { episodes(date: \"" + lastEpisodeQueryDate + "\", channelId: " + resource.id + ") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                //var episodesByChannelPayload = new DabGraphQlPayload(episodesByChannelQuery, variables);
                //string JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", episodesByChannelPayload));
                //DabSyncService.Instance.Send(JsonIn);

                //Navigate to the appropriate player page 
                if (Device.Idiom == TargetIdiom.Tablet)
                {
                    await Navigation.PushAsync(new DabTabletPage(resource));
                }
                else
                {
                    //PopupNavigation.PushAsync(new AchievementsProgressPopup());
                    await Navigation.PushAsync(new DabEpisodesPage(resource));
                }

                selected.IsNotSelected = 1.0;
                GlobalResources.WaitStop();

                //Send info to Firebase analytics that user accessed a channel
                var infoJ = new Dictionary<string, string>();
                infoJ.Add("channel", resource.title);
                DependencyService.Get<IAnalyticsService>().LogEvent("player_channel_selected", infoJ);

                //TODO: Subscribe to a channel
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                var r = await DisplayAlert("Unexpected error.", "We ran into an unexpected problem getting the episode list. Please try again.", "OK", "Details");
                if (r)
                {
                    //Do nothing.
                }
                else
                {
                    await DisplayAlert("Error Details", ex.Message, "OK");
                }
            }
        }

        void TimedActions()
        {
            if (GlobalResources.Instance.IsLoggedIn)
            {
                //update token if needed
                if (!AuthenticationAPI.CheckToken())
                {
                    //Send request for new token
                    if (DabSyncService.Instance.IsConnected)
                    {
                        DabGraphQlVariables variables = new DabGraphQlVariables();
                        var exchangeTokenQuery = "mutation { updateToken(version: 1) { token } }";
                        var exchangeTokenPayload = new DabGraphQlPayload(exchangeTokenQuery, variables);
                        var tokenJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", exchangeTokenPayload));
                        DabSyncService.Instance.Send(tokenJsonIn);
                    }
                }

                //post actions logs
                Task.Run(async () =>
                {
                    await AuthenticationAPI.PostActionLogs(false);
                    await AuthenticationAPI.GetMemberData();
                });

            }

            //Download new episodes
            Task.Run(async () =>
            {
                await PlayerFeedAPI.DownloadEpisodes();
            });

        }

        protected override async void OnAppearing()
        {
            //Show toolbar items for android
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

            //Sample to show today's reading after a second
            //TODO: Replace this delay with reception of current episode data.
            //TODO: Run in more than debug/test mode
            bool shouldShowTodaysEpisode = false;

            if (GlobalResources.TestMode)
            {
                shouldShowTodaysEpisode = true;
            }
#if DEBUG
            shouldShowTodaysEpisode = true;
#endif
            if (shouldShowTodaysEpisode)
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(1000);
                //TODO: Replace this with user's preferred channel
                ShowTodaysEpisode(_resource);
                });
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

        private void ShowTodaysEpisode(Resource resource)
        {
            try
            {

                if (!todaysEpisodeVisible)
                {
                    //Shows today's reading section with the most recent episode from the designated channel
                    //Display Today's Reading when it's available


                    //Get channel and episode
                    dbSettings ChannelSettings = adb.Table<dbSettings>().Where(x => x.Key == "Channel").FirstOrDefaultAsync().Result;
                    if (ChannelSettings == null)
                    {
                        ChannelSettings = new dbSettings() { Key = "Channel" };
                    }

                    var ch = ChannelSettings.Value;//adb.Table<dbChannels>().Where(x => x.channelId == resource.id).FirstOrDefaultAsync().Result;
                    var ep = adb.Table<dbEpisodes>().Where(e => e.channel_code == ch).OrderByDescending(x => x.PubDate).FirstOrDefaultAsync().Result;

                    if (ep != null && ch != null)
                    {
                        double TodaysEpisodeHeight = 250; //TODO: may need to be different for tablets
                        todaysEpisodeVisible = true; //mark it as visible so this won't run again

                        Device.BeginInvokeOnMainThread((() =>
                        {
                            TodaysEpisodeContentContainer.Padding = 25; //can't set that until in code to keep it hidden until now;
                            TodaysEpisodeContentContainer.Spacing = 18;
                            TodaysEpisodeTitle.Text = $"Today's Reading: {ep.title}";
                            TodaysEpisodePassageLabel.Text = ep.description;
                            TodaysEpisodeBackgroundImage.Source = resource.images.bannerPhone;
                        }
                        ));

                        //Animate today's reading
                        TodaysEpisodeContainer.HeightTo(0, TodaysEpisodeHeight, h => TodaysEpisodeContainer.HeightRequest = h, 500, Easing.SinIn);

                        //Set up button
                        TodaysEpisodeButton.Clicked += async (sender, e) =>
                        {
                            GlobalResources.WaitStart("Getting today's episode...");
                            var _reading = await PlayerFeedAPI.GetReading(ep.read_link);
                            if (ep.File_name_local != null || CrossConnectivity.Current.IsConnected)
                            {
                                await Navigation.PushAsync(new DabPlayerPage(ep, _reading));
                            }
                            GlobalResources.WaitStop();
                        };

                    }
                }
            }
            catch (Exception ex)
            {
                todaysEpisodeVisible = false; //try again later
            }






        }
    }
}
