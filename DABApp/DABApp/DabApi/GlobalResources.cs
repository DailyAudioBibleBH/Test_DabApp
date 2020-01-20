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
                //New - make sure this is the right place to change
                return "20200117-AddedUserEpisodeMeta-b";
                //return "20191210-AddedUserEpisodeMeta-b";
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

        public static string GetUserEmail()
        {
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

        //public static DateTime LastEpisodeQueryDate
        //{
        //    //Last episode query date by channel in GMT
        //    get
        //    {
        //        dbSettings LastEpisodeQuerySettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "EpisodeQueryDate");
        //        if (LastEpisodeQuerySettings == null)
        //        {
        //            DateTime episodeQueryDate = DateTime.UtcNow;
        //            LastEpisodeQuerySettings = new dbSettings();
        //            LastEpisodeQuerySettings.Key = "LastEpisodeQueryDate";
        //            LastEpisodeQuerySettings.Value = episodeQueryDate.ToString();
        //            db.InsertOrReplace(LastEpisodeQuerySettings);
        //            return episodeQueryDate;
        //        }
        //        else
        //        {
        //            return DateTime.Parse(LastEpisodeQuerySettings.Value);
        //        }

        //    }

        //    set
        //    {
        //        //Store the value sent to database
        //        string episodeQueryDate = value.ToString();
        //        dbSettings LastEpisodeQuerySettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "EpisodeQueryDate");
        //        LastEpisodeQuerySettings.Key = "LastEpisodeQueryDate";
        //        LastEpisodeQuerySettings.Value = episodeQueryDate;
        //        db.InsertOrReplace(LastEpisodeQuerySettings);
        //    }
        //}

        //Handled LastEpisodeQueryDate_{ChannelId} with methods instead of fields so I take in ChannelId
        public static DateTime GetLastEpisodeQueryDate(int ChannelId)
        {
            //Last episode query date by channel in GMT
            dbSettings LastEpisodeQuerySettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "EpisodeQueryDate" + ChannelId);
            
            
            return DateTime.MinValue.ToUniversalTime();
            //uncomment line once done testing
            //return DateTime.Parse(LastEpisodeQuerySettings.Value);
            
        }

        public static void SetLastEpisodeQueryDate(int ChannelId)
        {
            dbSettings LastEpisodeQuerySettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "EpisodeQueryDate" + ChannelId);

            //figure why this has always been true so far
            if (LastEpisodeQuerySettings == null)
            {
                DateTime episodeQueryDate = DateTime.MinValue.ToUniversalTime();
                LastEpisodeQuerySettings = new dbSettings();
                LastEpisodeQuerySettings.Key = "LastEpisodeQueryDate" + ChannelId;
                LastEpisodeQuerySettings.Value = DateTime.UtcNow.ToString();
                db.InsertOrReplace(LastEpisodeQuerySettings);
            }
            else
            {
                //Store the value sent to database
                string episodeQueryDate = DateTime.UtcNow.ToString();
                LastEpisodeQuerySettings.Key = "LastEpisodeQueryDate" + ChannelId;
                LastEpisodeQuerySettings.Value = episodeQueryDate;
                db.InsertOrReplace(LastEpisodeQuerySettings);
            }
        }

        public static DateTime LastActionDate
           //Last action check date in GMT (get/set universal time)
        {
            get
            {
                dbSettings LastActionsSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "ActionDate");
                
                if (LastActionsSettings == null)
                {
                    DateTime actionDate = DateTime.MinValue.ToUniversalTime();
                    LastActionsSettings = new dbSettings();
                    LastActionsSettings.Key = "ActionDate";
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
                string actionDate = value.ToString();
                dbSettings LastActionsSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "ActionDate");
                LastActionsSettings.Key = "ActionDate";
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

        public static bool ShouldUseSplitScreen
        {
            get
            {
                if(Device.Idiom == TargetIdiom.Phone || Device.RuntimePlatform=="Android")
                {
                    //Phones and Android Devices
                    return false;
                } else
                {
                    //iPad's only - eventually - we want this to be true
                    //TODO: Return True for Ipads
                    return false;
                }
            }
        }
    }

}
