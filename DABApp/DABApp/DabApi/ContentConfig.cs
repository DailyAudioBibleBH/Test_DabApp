using System;
using System.Collections.Generic;

namespace DABApp
{
	public class Data
	{
		public string updated { get; set; }
	}

	public class Nav
	{
		public string title { get; set; }
		public int view { get; set; }
	}

	public class Blocktext
	{
		public string appInfo { get; set; }
		public string termsAndConditions { get; set; }
		public string resetPassword { get; set; }
		public string signUp { get; set; }
		public string login { get; set; }
	}

	public class Images
	{
		public string thumbnail { get; set; }
		public string bannerPhone { get; set; }
		public string backgroundPhone { get; set; }
		public string backgroundTablet { get; set; }
	}

	public class Resource
	{
		public string title { get; set; }
		public string description { get; set; }
		public Images images { get; set; }
		public string feedUrl { get; set; }
		public string type { get; set; }
		public bool availableOffline { get; set; } = false;
	}

	public class Banner
	{
		public string type { get; set; }
		public string urlPhone { get; set; }
		public string urlTablet { get; set; }
		public string content { get; set; }
	}

	public class Link
	{
		public string title { get; set; }
		public string type { get; set; }
		public string urlPhone { get; set; }
		public string urlTablet { get; set; }
		public string link { get; set; }
		public string linkText { get; set; }
	}

	public class View
	{
		public string type { get; set; }
		public List<Resource> resources { get; set; }
		public int id { get; set; }
		public string title { get; set; }
		public Banner banner { get; set; }
		public string description { get; set; }
		public string content { get; set; }
		public List<View> children { get; set; }
		public List<Link> links { get; set; }
	}

	public class ContentConfig
	{
		public Data data { get; set; }
		public List<Nav> nav { get; set; }
		public Blocktext blocktext { get; set; }
		public List<View> views { get; set; }
		public static ContentConfig Instance { get; set;}
		static ContentConfig() {
			Instance = new ContentConfig();
		}
	}
}
