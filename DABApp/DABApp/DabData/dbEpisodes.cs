﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace DABApp
{
	public class dbEpisodes
	{
		[PrimaryKey]
		[Indexed]
		public int id { get; set;}
		public string title { get; set;}
		public string description { get; set;}
		public string author { get; set;}
		[Indexed]
		public DateTime PubDate { get; set;}
		[Indexed]
		public int PubDay { get; set;}
		[Indexed]
		public string PubMonth { get; set;}
		[Indexed]
		public int PubYear { get; set;}
		public string url { get; set;}
		public string read_link { get; set;}
		public string read_version_tag { get; set;}
		public string read_version_name { get; set;}
		[Indexed]
		public string channel_code { get; set;}
		public string channel_title { get; set;}
		public string channel_description { get; set;}
		public bool is_downloaded { get; set; } = false;
		public string file_name { get; set;}
		public bool is_listened_to { get; set; } = false;
		public double start_time { get; set; } = 0;
		public double stop_time { get; set; } = 0;

		[Ignore]
		public bool downloadVisible { 
			get {
				return is_downloaded;
			}
			set {
				is_downloaded = value;
				OnPropertyChanged("downloadVisible");
			}
		}

		[Ignore]
		public bool listenedToVisible { 
			get {
				return !is_listened_to;
			}
			set{
				is_listened_to = !value;
				OnPropertyChanged("listenedToVisible");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null) { 
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

		private static void NotifyStaticPropertyChanged(string propertyName)
		{
			StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
	}
}
