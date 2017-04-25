using System;
using SQLite;

namespace DABApp
{
	public class dbPlayerActions
	{
		[PrimaryKey]
		[AutoIncrement]
		public int id { get; set; }
		public DateTimeOffset ActionDateTime {get; set;}
		public string entity_type { get; set;}
		public int EpisodeId { get; set;}
		public string ActionType { get; set;}
		public decimal PlayerTime { get; set;}
		public string UserEmail { get; set;}
	}
}
