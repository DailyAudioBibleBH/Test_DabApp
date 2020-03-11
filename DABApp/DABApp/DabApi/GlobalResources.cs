using System;
using Xamarin.Forms;
using SlideOverKit;
using SQLite;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using DABApp.DabAudio;

namespace DABApp
{
    public class GlobalResources : INotifyPropertyChanged
    {
        public static DabPlayer playerPodcast = new DabPlayer(true);
        public static DabPlayer playerRecorder = new DabPlayer(false);
        public static int CurrentEpisodeId = 0;
        private double thumbnailHeight;
        private int flowListViewColumns = Device.Idiom == TargetIdiom.Tablet ? 3 : 2;
        public static readonly TimeSpan ImageCacheValidity = TimeSpan.FromDays(31); //Cache images for a month.

        static SQLiteConnection db = DabData.database;


        /* This string determins the database version. 
         * Any time you change this value and publish a release, a new database will be created and all other .db3 files will be removed
         */
        public static string DBVersion 
        {
            get
            {
                return "20200245e";
            }
        }

        public static string APIVersion { get; set; } = "2";

        public static readonly string APIKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvZGFpbHlhdWRpb2JpYmxlLmNvbSIsImlhdCI6MTUwOTQ3NTI5MywibmJmIjoxNTA5NDc1MjkzLCJleHAiOjE2NjcxNTUyOTMsImRhdGEiOnsidXNlciI6eyJpZCI6IjEyOTE4In19fQ.SKRNqrh6xlhTgONluVePhNwwzmVvAvUoAs0p9CgFosc";

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
                    return "https://feed.dailyaudiobible.com/wp-json/lutd/v1/";
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
                    return "https://feed.dailyaudiobible.com/wp-json/lutd/v1/";
                }


            }
        }
        public bool IsiPhoneX { get; set; } = false;

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


        public static string GetUserEmail()
        {
            var settings = db.Table<dbSettings>().ToList();
            dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
            if (EmailSettings == null)
            {
                return "";
            }
            else
            {
                return EmailSettings.Value;
            }
        }

        public static string GetUserName()
        {
            dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
            dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
            if (FirstNameSettings == null || LastNameSettings == null)
            {
                return "";
            }
            else
            {
                return FirstNameSettings.Value + " " + LastNameSettings.Value;
            }
        }

        //Handled LastEpisodeQueryDate_{ChannelId} with methods instead of fields so I take in ChannelId
        public static string GetLastEpisodeQueryDate(int ChannelId)
        {
            //Last episode query date by channel in GMT
            dbSettings LastEpisodeQuerySettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "EpisodeQueryDate" + ChannelId);
            if (LastEpisodeQuerySettings == null)
            {
                DateTime queryDate = DateTime.MinValue.ToUniversalTime();
                LastEpisodeQuerySettings = new dbSettings();
                LastEpisodeQuerySettings.Key = "EpisodeQueryDate" + ChannelId;
                LastEpisodeQuerySettings.Value = queryDate.ToString("o");
                db.InsertOrReplace(LastEpisodeQuerySettings);
                return queryDate.ToString("o");
            }
            else
            {
                return LastEpisodeQuerySettings.Value;
            }
        }

        public static void SetLastEpisodeQueryDate(int ChannelId)
        {
            dbSettings LastEpisodeQuerySettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "EpisodeQueryDate" + ChannelId);

            //Store the value sent in the database
            string queryDate = DateTime.UtcNow.ToString("o");
            LastEpisodeQuerySettings.Key = "EpisodeQueryDate" + ChannelId;
            LastEpisodeQuerySettings.Value = queryDate;
            db.InsertOrReplace(LastEpisodeQuerySettings);
        }

        //Last badge check date in GMT (get/set universal time)
        public static DateTime BadgesUpdatedDate
        {
            get
            {
                string settingsKey = "BadgeUpdateDate";
                dbSettings BadgeUpdateSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);

                if (BadgeUpdateSettings == null)
                {
                    DateTime badgeDate = DateTime.MinValue.ToUniversalTime();
                    BadgeUpdateSettings = new dbSettings();
                    BadgeUpdateSettings.Key = settingsKey;
                    BadgeUpdateSettings.Value = badgeDate.ToString();
                    db.InsertOrReplace(BadgeUpdateSettings);
                    return badgeDate;
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
                dbSettings BadgeUpdateSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);
                BadgeUpdateSettings.Key = settingsKey;
                BadgeUpdateSettings.Value = badgeDate;
                db.InsertOrReplace(BadgeUpdateSettings);
            }
        }

        public static DateTime BadgeProgressUpdatesDate
        //Last badge progress check date in GMT (get/set universal time)
        {
            get
            {
                string settingsKey = $"BadgeProgressDate-{GlobalResources.GetUserEmail()}";
                dbSettings BadgeProgressSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);

                if (BadgeProgressSettings == null)
                {
                    DateTime progressDate = DateTime.MinValue.ToUniversalTime();
                    BadgeProgressSettings = new dbSettings();
                    BadgeProgressSettings.Key = settingsKey;
                    BadgeProgressSettings.Value = progressDate.ToString();
                    db.InsertOrReplace(BadgeProgressSettings);
                    return progressDate;
                }
                else
                {
                    return DateTime.Parse(BadgeProgressSettings.Value);
                }
            }

            set
            {
                //Store the value sent in the database
                string settingsKey = $"BadgeProgressDate-{GlobalResources.GetUserEmail()}";
                string progressDate = value.ToString();
                dbSettings BadgeProgressSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);
                BadgeProgressSettings.Key = settingsKey;
                BadgeProgressSettings.Value = progressDate;
                db.InsertOrReplace(BadgeProgressSettings);
            }
        }

        public static DateTime LastActionDate
        //Last action check date in GMT (get/set universal time)
        {
            get
            {
                string settingsKey = $"ActionDate-{GlobalResources.GetUserEmail()}";
                dbSettings LastActionsSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);
                
                if (LastActionsSettings == null)
                {
                    DateTime actionDate = DateTime.MinValue.ToUniversalTime();
                    LastActionsSettings = new dbSettings();
                    LastActionsSettings.Key = settingsKey;
                    LastActionsSettings.Value = actionDate.ToString();
                    db.InsertOrReplace(LastActionsSettings);
                    return actionDate;
                }
                else
                {
                    return DateTime.Parse(LastActionsSettings.Value);
                }
            }

            set
            {
                //Store the value sent in the database
                string settingsKey = $"ActionDate-{GlobalResources.GetUserEmail()}";
                string actionDate = value.ToString();
                dbSettings LastActionsSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);
                LastActionsSettings.Key = settingsKey;
                LastActionsSettings.Value = actionDate;
                db.InsertOrReplace(LastActionsSettings);
            }
        }

        public static string UserAvatar
        {
            get
            {
                dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
                if (AvatarSettings == null)
                {
                    return "";
                }
                else return AvatarSettings.Value;
            }
        }

        public static string GetUserAvatar()
        {
            dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
            if (AvatarSettings == null)
            {
                return "";
            }
            else return AvatarSettings.Value;
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
    }
}
