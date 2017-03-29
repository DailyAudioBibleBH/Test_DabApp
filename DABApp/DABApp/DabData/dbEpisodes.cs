using System;
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
		public int PubMonth { get; set;}
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
		public string is_listened_to { get; set;}
		public double start_time { get; set; } = 0;
		public double stop_time { get; set; } = 0;
	}
}
