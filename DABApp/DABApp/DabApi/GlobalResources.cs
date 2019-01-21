using System;
using Xamarin.Forms;
using SlideOverKit;
using SQLite;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace DABApp
{
	public class GlobalResources : INotifyPropertyChanged
	{
        //public static bool IsPlaying { get; set; }
        //public static IAudio Player { get; set;}

        private double thumbnailHeight;
        private int flowListViewColumns = Device.Idiom == TargetIdiom.Tablet ? 3 : 2;
		public static readonly TimeSpan ImageCacheValidity = TimeSpan.FromDays(31); //Cache images for a month.

		static SQLiteConnection db = DabData.database;


        /* This string determins the database version. 
         * Any time you change this value and publish a release, a new database will be created and all other .db3 files will be removed
         */
        public static string DBVersion
        {
            get {
                return "1.0";
            }
        }

        public static string APIVersion { get; set; } = "2";

		public static readonly string APIKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvZGFpbHlhdWRpb2JpYmxlLmNvbSIsImlhdCI6MTUwOTQ3NTI5MywibmJmIjoxNTA5NDc1MjkzLCJleHAiOjE2NjcxNTUyOTMsImRhdGEiOnsidXNlciI6eyJpZCI6IjEyOTE4In19fQ.SKRNqrh6xlhTgONluVePhNwwzmVvAvUoAs0p9CgFosc";

        public event PropertyChangedEventHandler PropertyChanged;

        public static string RestAPIUrl {
            get {
                var main = TestMode ? ContentConfig.Instance.app_settings.stage_main_link : ContentConfig.Instance.app_settings.prod_main_link;
                return main + "/wp-json/lutd/v1/";
            }
        }
        public static string FeedAPIUrl { get {
                if (ContentConfig.Instance.app_settings == null) return "https://feed.dailyaudiobible.com/wp-json/lutd/v1/";
                var feed = TestMode ? ContentConfig.Instance.app_settings.stage_feed_link : ContentConfig.Instance.app_settings.prod_feed_link;
                return feed + "/wp-json/lutd/v1/";
            }
        }
        public bool IsiPhoneX { get; set; } = false;

		public static GlobalResources Instance {get; private set;}

        public bool OnRecord { get; set; }

		static GlobalResources(){
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
				else {
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

		public static string GetUserAvatar() { 
			dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
			if (AvatarSettings == null)
			{
				return "";
			}
			else return AvatarSettings.Value;
		}

        public static bool TestMode { get; set; }
        public static string GiveUrl {
            get
            {
                return TestMode ? ContentConfig.Instance.app_settings.stage_give_link + "/" : ContentConfig.Instance.app_settings.prod_give_link + "/";
            }
        }
        public static string JournalUrl {
            get {
                return TestMode ? ContentConfig.Instance.app_settings.stage_journal_link + "/" : ContentConfig.Instance.app_settings.prod_journal_link + "/";
            }
        }
		public static bool LogInPageExists { get; set; }
		public static bool DeleteEpisodesAfterListening { get; set; }
		public static string DurationPicked { get; set; }
        public int ScreenSize { get; set; }
        public float AndroidDensity { get; set; }
        public List<PodcastEmail> PodcastEmails { get; set; } = new List<PodcastEmail>()
                {
                    new PodcastEmail() { Podcast = "Daily Audio Bible", Email = "dab@c2itconsulting.net"},
                    new PodcastEmail() { Podcast = "Daily Audio Bible Chronological", Email = "dab@c2itconsulting.net"}
        };
    }

}
