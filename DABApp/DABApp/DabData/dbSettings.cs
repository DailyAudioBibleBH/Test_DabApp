using System;
using SQLite;

namespace DABApp
{
	public class dbSettings
	{
		[PrimaryKey]
		public string Key { get; set;}
		public string Value { get; set;}
	}
}
