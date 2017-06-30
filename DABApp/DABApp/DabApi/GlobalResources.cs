﻿using System;
using Xamarin.Forms;
using SlideOverKit;
using SQLite;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DABApp
{
	public class GlobalResources
	{
		//public static bool IsPlaying { get; set; }
		//public static IAudio Player { get; set;}

		public static readonly TimeSpan ImageCacheValidity = TimeSpan.FromDays(31); //Cache images for a month.

		static SQLiteConnection db = DabData.database;

		public static GlobalResources Instance {get; private set;}

		static GlobalResources(){
			Instance = new GlobalResources();
		}

		public int FlowListViewColumns
		{
			//Returns the number of columnts to use in a FlowListView
			get
			{
				switch (Device.Idiom)
				{
					case TargetIdiom.Phone:
						return 2;
					case TargetIdiom.Tablet:
						return 3;
					default:
						return 2;
				}
			}
		}


		public double ThumbnailImageHeight
		{
			//returns the height we should use for a square thumbnail (based on the idiom and screen WIDTH)
			get
			{
				double knownPadding = 30;
				if (App.Current.MainPage != null)
				{
					return (App.Current.MainPage.Width / FlowListViewColumns) - knownPadding;
				}
				else {
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						return 212;
					}
					else return 180;
				}
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

		public static bool LogInPageExists { get; set; }
		public static bool DeleteEpisodesAfterListening { get; set; }
		public static string DurationPicked { get; set; }
	}

}
