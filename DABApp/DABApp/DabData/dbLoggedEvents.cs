using System;
using SQLite;

namespace DABApp
{
	public class dbLoggedEvents
	{
		[PrimaryKey]
		[AutoIncrement]
		public int id { get; set; }
		[Indexed]
		public DateTime entity_datetime { get; set; }
		public string entityType {get; set;}
		public string entityId { get; set;}
		public string action { get; set;}
		public double playerTime { get; set;}
	}
}
