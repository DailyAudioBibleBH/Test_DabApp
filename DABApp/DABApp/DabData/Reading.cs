using System;
using System.Collections.Generic;

namespace DABApp
{
	public class Reading
	{
		public string title { get; set;}
		public string link { get; set; }
		public string text { get; set;}
		public List<string> excerpts { get; set;}
		public int id { get; set;}
		public bool IsAlt { get; set; } = false;
		public string message { get; set; } = null;
	}
}
