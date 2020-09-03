using System;
using Xamarin.Forms;
using SlideOverKit;
using SQLite;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using DABApp.DabAudio;
using DABApp.DabSockets;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.Service;
using Xamarin.Essentials;

namespace DABApp
{
    public class GlobalResources : INotifyPropertyChanged
    {
        public static DabPlayer playerPodcast = new DabPlayer(true);
        public static DabPlayer playerRecorder = new DabPlayer(false);
        public static List<dbSettings> SettingsToPreserve; //List of settings to be preserved when a database version is made (see sqlite_{platform}.cs)
        public static int CurrentEpisodeId = 0;
        private double thumbnailHeight;
        private int flowListViewColumns = Device.Idiom == TargetIdiom.Tablet ? 3 : 2;
        public static readonly TimeSpan ImageCacheValidity = TimeSpan.FromDays(31); //Cache images for a month.

        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;

        public GlobalResources()
        {
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        async void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            var access = e.NetworkAccess;
            var profiles = e.ConnectionProfiles;
            var current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
            {
                Debug.WriteLine("Gained internet access");
                DabServiceEvents.TrafficOccured(GraphQlTrafficDirection.Connected, "connected");
                // Connection to internet is available
                // If websocket is not connected, reconnect
                if (!DabService.IsConnected)
                {
                    //reconnect to service
                    var ql = await DabService.InitializeConnection();

                    if (ql.Success)
                    {
                        //perform post-connection operations with service
                        await DabServiceRoutines.RunConnectionEstablishedRoutines();
                    }
                }
            }
            else
            {
                Debug.WriteLine("Lost internet access");
                DabServiceEvents.TrafficOccured(GraphQlTrafficDirection.Disconnected, "disconnected");
                await DabService.TerminateConnection();
            }
        }


        /* This string determins the database version. 
         * Any time you change this value and publish a release, a new database will be created and all other .db3 files will be removed
         */
        public static string DBVersion
        {
            get
            {
                return "20200716";
            }
        }

        public static int PullToRefreshRate
        {
            get
            {
                return 1;
            }
        }

        public static DateTime DabMinDate //The min date we use throughout the DAB app
        { get
            {
                return new DateTime(2019, 12, 31);
            }
        }

        public static string APIVersion { get; set; } = "2";

        public static readonly string APIKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvZGFpbHlhdWRpb2JpYmxlLmNvbSIsImlhdCI6MTU4OTk5NDcxOSwibmJmIjoxNTg5OTk0NzE5LCJleHAiOjE3NDc2NzQ3MTksImRhdGEiOnsidXNlciI6eyJpZCI6IjEyOTE4In19fQ.JCt2vuC2tSkyY2Y5YUFZK6DpQ9I_EoVt3KAUqrzQQ0A";
        public static readonly string StripeApiKey = "pk_live_O0E92mb0sHFrAD5JGBiU9fgK";


        //old api tokens
        //public static readonly string APIKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvZGFpbHlhdWRpb2JpYmxlLmNvbSIsImlhdCI6MTUwOTQ3NTI5MywibmJmIjoxNTA5NDc1MjkzLCJleHAiOjE2NjcxNTUyOTMsImRhdGEiOnsidXNlciI6eyJpZCI6IjEyOTE4In19fQ.SKRNqrh6xlhTgONluVePhNwwzmVvAvUoAs0p9CgFosc";

        //public static readonly string APIRoleKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvZGFpbHlhdWRpb2JpYmxlLmNvbSIsImlhdCI6MTU4OTk5NDcxOSwibmJmIjoxNTg5OTk0NzE5LCJleHAiOjE3NDc2NzQ3MTksImRhdGEiOnsidXNlciI6eyJpZCI6IjEyOTE4In19fQ.JCt2vuC2tSkyY2Y5YUFZK6DpQ9I_EoVt3KAUqrzQQ0A";


        public event PropertyChangedEventHandler PropertyChanged;

        public static string RestAPIUrl
        {
            get
            {
                try
                {
                    //Ensure we have content config data. If not, use hard coded value.
                    if (ContentConfig.Instance.app_settings == null)
                    {
                        throw new Exception("No Content Config - Use last known feed url");
                    }

                    string url;
                    if (TestMode)
                    {
                        url = ContentConfig.Instance.app_settings.stage_main_link;
                    }
                    else
                    {
                        url = ContentConfig.Instance.app_settings.prod_main_link;
                    }
                    return url + "/wp-json/lutd/v1/";
                }
                catch (Exception ex)
                {
                    //hard coded default content api paths
                    if (TestMode)
                    {
                        return "https://feed.staging.dailyaudiobible.com/wp-json/lutd/v1/";
                    }
                    else
                    {
                        return "https://feed.dailyaudiobible.com/wp-json/lutd/v1/";
                    }
                }

            }
        }


        public static string FeedAPIUrl
        {
            get
            {
                try
                {

                    //Ensure we have content config data. If not, use hard coded value.
                    if (ContentConfig.Instance.app_settings == null)
                    {
                        throw new Exception("No Content Config - Use last known feed url");
                    }

                    //Otherwise use production / test mode URLs
                    string url;
                    if (TestMode)
                    {
                        url = ContentConfig.Instance.app_settings.stage_feed_link;
                    }
                    else
                    {
                        url = ContentConfig.Instance.app_settings.prod_feed_link;
                    }

                    return url + "/wp-json/lutd/v1/";
                }
                catch (Exception ex)
                {
                    //hard coded default content api paths
                    if (TestMode)
                    {
                        return "https://feed.staging.dailyaudiobible.com/wp-json/lutd/v1/";
                    }
                    else
                    {
                        return "https://feed.dailyaudiobible.com/wp-json/lutd/v1/";
                    }
                }


            }
        }
        public bool IsiPhoneX { get; set; } = false;

        //Instance to find if user is logged in or not
        public bool IsLoggedIn
        {
            get
            {
                return !GuestStatus.Current.IsGuestLogin;
            }

        }
        public static GlobalResources Instance { get; private set; }

        public bool OnRecord { get; set; }

        static GlobalResources()
        {
            Instance = new GlobalResources();
        }

        public int FlowListViewColumns
        {
            //Returns the number of columnts to use in a FlowListView
            get
            {
                return flowListViewColumns;
            }
            set
            {
                flowListViewColumns = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FlowListViewColumns"));
            }
        }


        public double ThumbnailImageHeight
        {
            //returns the height we should use for a square thumbnail (based on the idiom and screen WIDTH)
            get
            {
                //double knownPadding = 30;
                if (App.Current.MainPage != null)
                {
                    return thumbnailHeight;
                }
                else
                {
                    if (Device.Idiom == TargetIdiom.Tablet)
                    {
                        return thumbnailHeight = 212;
                    }
                    else return thumbnailHeight = 180;
                }
            }
            set
            {
                thumbnailHeight = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ThumbnailImageHeight"));
            }
        }

        public static bool ShouldUseSplitScreen
        {
            get
            {
                if (Device.Idiom == TargetIdiom.Phone || Device.RuntimePlatform == "Android")
                {
                    //Phones and Android Devices
                    return false;
                }
                else
                {
                    //iPad's only - eventually - we want this to be true
                    //TODO: Return True for Ipads
                    return false;
                }
            }
        }

        public static void SetDisplay()
        {
            var display = adb.Table<dbSettings>().Where(x => x.Key == "Display").FirstOrDefaultAsync().Result;
            if (display != null)
            {
                if (display.Value == "LightMode")
                {
                    App.Current.Resources["InputBackgroundColor"] = Color.FromHex("#FFFFFF");
                    App.Current.Resources["PageBackgroundColor"] = Color.FromHex("#FFFFFF");
                    App.Current.Resources["TextColor"] = Color.FromHex("#FFFFFF");
                    App.Current.Resources["NavBarBackgroundColor"] = Color.FromHex("#FFFFFF");
                    App.Current.Resources["SlideMenuBackgroundColor"] = Color.FromHex("#FFFFFF");
                }
                else if (display.Value == "DarkMode")
                {
                    App.Current.Resources["InputBackgroundColor"] = Color.FromHex("#444444");
                    App.Current.Resources["PageBackgroundColor"] = Color.FromHex("#292929");
                    App.Current.Resources["TextColor"] = Color.FromHex("#CBCBCB");
                    App.Current.Resources["NavBarBackgroundColor"] = Color.FromHex("#383838");
                    App.Current.Resources["SlideMenuBackgroundColor"] = Color.FromHex("#D5272E");
                }
                else
                {
                    App.Current.Resources["InputBackgroundColor"] = Color.FromHex("#444444");
                    App.Current.Resources["PageBackgroundColor"] = Color.FromHex("#292929");
                    App.Current.Resources["TextColor"] = Color.FromHex("#CBCBCB");
                    App.Current.Resources["NavBarBackgroundColor"] = Color.FromHex("#383838");
                    App.Current.Resources["SlideMenuBackgroundColor"] = Color.FromHex("#D5272E");
                }
                adb.InsertOrReplaceAsync(display);
            }
        }

        public static string GetUserWpId()
        {
            try
            {

                if (!GuestStatus.Current.IsGuestLogin)
                {
                    return dbSettings.GetSetting("WpId", "-1");
                }
                else
                {
                    return "0"; //guest
                }
            }
            catch (Exception ex)
            {
                return "-2"; //error
            }

        }

        public static string GetUserName()
        {
            //friendly user name
            return (dbSettings.GetSetting("FirstName", "") + " " + dbSettings.GetSetting("LastName", "")).Trim();
        }

        //Handled LastEpisodeQueryDate_{ChannelId} with methods instead of fields so I take in ChannelId
        public static DateTime GetLastEpisodeQueryDate(int ChannelId)
        {
            //Last episode query date by channel in GMT
            string k = "EpisodeQueryDate" + ChannelId;
            DateTime querydate = DateTime.Parse(dbSettings.GetSetting(k, DabMinDate.ToUniversalTime().ToString()));
            return querydate;
        }

        public static void SetLastEpisodeQueryDate(int ChannelId, DateTime LastDate)
        {
            string k = "EpisodeQueryDate" + ChannelId;
            Debug.WriteLine($"Setting last episode query date for Channel {ChannelId}: { LastDate.ToString()}");
            dbSettings.StoreSetting(k, $"{LastDate.ToString()}");
        }

        //Last badge check date in GMT (get/set universal time)
        public static DateTime BadgesUpdatedDate
        {
            get
            {
                string settingsKey = "BadgeUpdateDate";
                dbSettings BadgeUpdateSettings = adb.Table<dbSettings>().Where(x => x.Key == settingsKey).FirstOrDefaultAsync().Result;

                if (BadgeUpdateSettings == null)
                {
                    DateTime badgeDate = GlobalResources.DabMinDate.ToUniversalTime();
                    BadgeUpdateSettings = new dbSettings();
                    BadgeUpdateSettings.Key = settingsKey;
                    BadgeUpdateSettings.Value = badgeDate.ToString();
                    var x = adb.InsertOrReplaceAsync(BadgeUpdateSettings).Result;
                    return DateTime.Parse(BadgeUpdateSettings.Value);
                }
                else
                {
                    return DateTime.Parse(BadgeUpdateSettings.Value);
                }
            }
            set
            {
                //Store the value sent in the database
                string settingsKey = "BadgeUpdateDate";
                string badgeDate = value.ToString();
                dbSettings BadgeUpdateSettings = adb.Table<dbSettings>().Where(x => x.Key == settingsKey).FirstOrDefaultAsync().Result;
                BadgeUpdateSettings.Key = settingsKey;
                BadgeUpdateSettings.Value = badgeDate;
                var x = adb.InsertOrReplaceAsync(BadgeUpdateSettings).Result;
            }
        }

        public static DateTime BadgeProgressUpdatesDate
        //Last badge progress check date in GMT (get/set universal time)
        {
            get
            {
                string settingsKey = $"BadgeProgressDate-{dbSettings.GetSetting("Email","")}";
                dbSettings BadgeProgressSettings = adb.Table<dbSettings>().Where(x => x.Key == settingsKey).FirstOrDefaultAsync().Result;

                if (BadgeProgressSettings == null)
                {
                    DateTime progressDate = GlobalResources.DabMinDate.ToUniversalTime();
                    BadgeProgressSettings = new dbSettings();
                    BadgeProgressSettings.Key = settingsKey;
                    BadgeProgressSettings.Value = progressDate.ToString();
                    var x = adb.InsertOrReplaceAsync(BadgeProgressSettings).Result;
                    return DateTime.Parse(BadgeProgressSettings.Value);
                }
                else
                {
                    return DateTime.Parse(BadgeProgressSettings.Value);
                }
            }

            set
            {
                //Store the value sent in the database
                string settingsKey = $"BadgeProgressDate-{dbSettings.GetSetting("Email", "")}";
                string progressDate = value.ToString();
                dbSettings BadgeProgressSettings = adb.Table<dbSettings>().Where(x => x.Key == settingsKey).FirstOrDefaultAsync().Result;
                BadgeProgressSettings.Key = settingsKey;
                BadgeProgressSettings.Value = progressDate;
                var x = adb.InsertOrReplaceAsync(BadgeProgressSettings).Result;
            }
        }

        public static DateTime LastActionDate
        //Last action check date in GMT (get/set universal time)
        {
            get
            {
                string settingsKey = $"ActionDate-{dbSettings.GetSetting("Email", "")}";
                DateTime LastActionDate = DateTime.Parse(dbSettings.GetSetting(settingsKey, DabMinDate.ToString()));
                return LastActionDate;
            }

            set
            {
                //Store the value sent in the database
                string settingsKey = $"ActionDate-{dbSettings.GetSetting("Email", "")}";
                string actionDate = value.ToString();
                dbSettings.StoreSetting(settingsKey, actionDate);
            }
        }

        //Handled LastRefreshDate_{ChannelId} with methods instead of fields so I take in ChannelId
        public static string GetLastRefreshDate(int ChannelId)
        {
            //Last episode query date by channel in GMT
            string k = "RefreshDate" + ChannelId;
            dbSettings LastRefreshSettings = adb.Table<dbSettings>().Where(x => x.Key == k).FirstOrDefaultAsync().Result;
            if (LastRefreshSettings == null)
            {
                DateTime refreshDate = GlobalResources.DabMinDate.ToUniversalTime();
                LastRefreshSettings = new dbSettings();
                LastRefreshSettings.Key = k;
                LastRefreshSettings.Value = refreshDate.ToString("o");
                var x = adb.InsertOrReplaceAsync(LastRefreshSettings).Result;
                return refreshDate.ToString("o");
            }
            else
            {
                return LastRefreshSettings.Value;
            }
        }
    

        public static void SetLastRefreshDate(int ChannelId)
        {
            string k = "RefreshDate" + ChannelId;
            dbSettings LastRefreshSettings = adb.Table<dbSettings>().Where(x => x.Key == k).FirstOrDefaultAsync().Result;

            //Store the value sent in the database
            string queryDate = DateTime.UtcNow.ToString("o");
            LastRefreshSettings.Key = k;
            LastRefreshSettings.Value = queryDate;
            var x = adb.InsertOrReplaceAsync(LastRefreshSettings).Result;
        }

        public static string UserAvatar
        {
            get
            {
                dbSettings AvatarSettings = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
                if (AvatarSettings == null)
                {
                    return "";
                }
                else return AvatarSettings.Value;
            }
        }

        public static string GetUserAvatar()
        {
            dbSettings AvatarSettings = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
            if (AvatarSettings == null)
            {
                return "";
            }
            else return AvatarSettings.Value;
        }

        //Get or set Test Mode
        public static bool TestMode { get; set; }

        //Get or set Experiment Mode
        public static bool ExperimentMode { get; set; }

        //Return the base URL to give
        public static string GiveUrl
        {
            get
            {
                try
                {
                    if (TestMode)
                    {
                        return ContentConfig.Instance.app_settings.stage_give_link + "/";
                    }
                    else
                    {
                        return ContentConfig.Instance.app_settings.prod_give_link + "/";
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        public static void WaitStart()
        {
            MessagingCenter.Send<string, string>("dabapp", "Wait_Start", "Please Wait...");
        }

        public static void WaitStart(string message, bool ShowDismissButton)
        {
            MessagingCenter.Send<string, string>("dabapp", "Wait_Start_WithoutDismiss", message);
        }

        public static void WaitStart(string message)
        {
            MessagingCenter.Send<string, string>("dabapp", "Wait_Start", message);
        }

        public static void WaitStop()
        {
            MessagingCenter.Send<string>("dabapp", "Wait_Stop");
        }



        //Return the base URL to connect to the Journal
        public static string JournalUrl
        {
            get
            {
                try
                {
                    if (TestMode)
                    {
                        return ContentConfig.Instance.app_settings.stage_journal_link + "/";
                    }
                    else
                    {
                        return ContentConfig.Instance.app_settings.prod_journal_link + "/";
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        public static bool LogInPageExists { get; set; }
        public static bool DeleteEpisodesAfterListening { get; set; }
        public static string DurationPicked { get; set; }
        public int ScreenSize { get; set; }
        public float AndroidDensity { get; set; }

        //Build an array of email destinations for various recording submissions
        public List<PodcastEmail> PodcastEmails { get; set; } = new List<PodcastEmail>()
                {
                    new PodcastEmail() { Podcast = "Daily Audio Bible", Email = "prayerapp@dailyaudiobible.com"},
                    new PodcastEmail() { Podcast = "Daily Audio Bible Chronological", Email = "china@dailyaudiobible.com"}
        };



        public static async void GoToRecordingPage()
        {
            //Takes the user to the recording page if they are logged in. If not, alerts them to log in first
            var nav = Application.Current.MainPage.Navigation;
            if (GuestStatus.Current.IsGuestLogin)
            {
                var r = await Application.Current.MainPage.DisplayAlert("Login Required", "You must be logged into use this feature.", "Login", "Cancel");
                if (r == true)
                {
                    GlobalResources.LogoffAndResetApp();
                }
            }
            else
            {
                //logged in user
                await nav.PushModalAsync(new DabRecordingPage());
            }
        }


        public static async Task<bool> LogoffAndResetApp(string Message = null)
        {
            //This method will log the current user off, reset all players and the app back to the login view, and reconnect all connections.
            //If Message is null, this will happen without any notification to the user. If a message is passed, it will be shown to the user and then they will be reset.
            //The user will NOT have the option to "cancel" the action

            if (Message != null)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Login Required", Message, "OK");
                }
                );
            }

            //Player.
            CurrentEpisodeId = 0;
            playerPodcast.Stop();

            //Database
            await dbSettings.DeleteLoginSettings();

            //Websocket
            await DabService.TerminateConnection();


            Device.BeginInvokeOnMainThread(() =>
            {
                //Reset main page of app.
                Application.Current.MainPage = new NavigationPage(new DabCheckEmailPage());
            });

            return true;
        }
    }
}
