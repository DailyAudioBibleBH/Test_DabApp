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
using System.Security.Cryptography;
using System.Text;

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
        {   get
            {
                int registerYear = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.UserRegistered.Year;
                int episodeYear = ContentConfig.Instance.options.episode_year;
                int minYear = Math.Max(registerYear, episodeYear);
                return new DateTime(minYear, 1 , 1);
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

        public static int GetUserWpId()
        {
            try
            {

                if (!GuestStatus.Current.IsGuestLogin)
                {
                    int wpID = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.WpId;
                    return wpID;
                }
                else
                {
                    return 0; //guest
                }
            }
            catch (Exception ex)
            {
                return -2; //error
            }

        }

        public static string GetUserName()
        {
            //friendly user name
            return (adb.Table<dbUserData>().FirstOrDefaultAsync().Result.FirstName + " " + adb.Table<dbUserData>().FirstOrDefaultAsync().Result.LastName);
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
                string BadgeUpdateSettingsValue = dbSettings.GetSetting(settingsKey, "");
                if (BadgeUpdateSettingsValue == "")
                {
                    DateTime badgeDate = GlobalResources.DabMinDate.ToUniversalTime();
                    dbSettings.StoreSetting(settingsKey, badgeDate.ToString());
                }
                return DateTime.Parse(dbSettings.GetSetting("BadgeUpdateDate", ""));
            }
            set
            {
                //Store the value sent in the database
                string settingsKey = "BadgeUpdateDate";
                string badgeDate = value.ToString();
                dbSettings.StoreSetting(settingsKey, badgeDate);
            }
        }

        public static DateTime BadgeProgressUpdatesDate
        //Last badge progress check date in GMT (get/set universal time)
        {
            get
            {
                return adb.Table<dbUserData>().FirstOrDefaultAsync().Result.ProgressDate;
            }

            set
            {
                //Store the value sent in the database
                dbUserData user = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;
                user.ProgressDate = value;
                adb.InsertOrReplaceAsync(user);
            }
        }

        public static DateTime LastActionDate
        //Last action check date in GMT (get/set universal time)
        {
            get
            {
                return adb.Table<dbUserData>().FirstOrDefaultAsync().Result.ActionDate;
            }

            set
            {
                //Store the value sent in the database
                dbUserData user = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;
                user.ActionDate = value;
                adb.InsertOrReplaceAsync(user);
            }
        }

        //Handled LastRefreshDate_{ChannelId} with methods instead of fields so I take in ChannelId
        public static string GetLastRefreshDate(int ChannelId)
        {
            //Last episode query date by channel in GMT
            string k = "RefreshDate" + ChannelId;
            //dbSettings LastRefreshSettings = adb.Table<dbSettings>().Where(x => x.Key == k).FirstOrDefaultAsync().Result;
            string LastRefreshSettingsValue = dbSettings.GetSetting(k, "");
            if (LastRefreshSettingsValue == "")
            {
                DateTime refreshDate = GlobalResources.DabMinDate.ToUniversalTime();
                dbSettings.StoreSetting(k, refreshDate.ToString("o"));
            }

            return dbSettings.GetSetting(k, "");
        }
    

        public static void SetLastRefreshDate(int ChannelId)
        {
            //Store the value sent in the database
            string k = "RefreshDate" + ChannelId;
            string queryDate = DateTime.UtcNow.ToString("o");
            dbSettings.StoreSetting(k, queryDate);
        }

        public static string UserAvatar
        {
            get
            {
                //request gravatar from gravatar.com if not custom gravatar set then use placeholder instead.
                string hash = CalculateMD5Hash(adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Email);
                return string.Format("https://www.gravatar.com/avatar/{0}?d=mp", hash);
            }
        }
        public static string CalculateMD5Hash(string email)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.  
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.  
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(email.ToLower().Trim()));

            // Create a new Stringbuilder to collect the bytes  
            // and create a string.  
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string.  
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();  // Return the hexadecimal string. 
        }
        
        //Get or set Test Mode
        public static bool TestMode { get; set; }

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
