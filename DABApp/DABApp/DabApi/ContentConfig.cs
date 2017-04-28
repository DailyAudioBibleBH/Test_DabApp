﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FFImageLoading;
using Xamarin.Forms;

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

	public class Resource: INotifyPropertyChanged
	{
		public int id { get; set;}
		public string title { get; set; }
		public string description { get; set; }
		public Images images { get; set; }
		public string feedUrl { get; set; }
		public string type { get; set; }
		public bool availableOffline { get; set; } = false;

		private bool _IsNotSelected = true;
		public bool IsNotSelected { 
			get {
				return _IsNotSelected;
			}
			set {
				_IsNotSelected = value;
				OnPropertyChanged("IsNotSelected");
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

		private static void NotifyStaticPropertyChanged(string propertyName)
		{
			StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
		}
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
		public bool HasGraphic
		{
			get
			{
				if (type == "text")
				{
					return false;
				}
				else return true;
			}
		}
		public bool HasNoGraphic { 
			get {
				if (type == "image")
				{
					return false;
				}
				else return true;
			}
		}
		public string PhoneOrTab { 
			get {
				if (Device.Idiom == TargetIdiom.Tablet)
				{
					return urlTablet;
				}
				else return urlPhone;
			}
		}
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

		public async void cachImages() 
		{ 
			var channelView = Instance.views.Single(x => x.title == "Channels");
			var initiativeView = Instance.views.Single(x => x.title == "Initiatives");
			try
			{
				foreach (var v in views) {
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await ImageService.Instance.LoadUrl(v.banner.urlTablet).DownloadOnlyAsync();
					}
					else {
						await ImageService.Instance.LoadUrl(v.banner.urlPhone).DownloadOnlyAsync();
					}
				}
				foreach (var r in channelView.resources)
				{
					var image = r.images;
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await ImageService.Instance.LoadUrl(image.backgroundTablet).DownloadOnlyAsync();
					}
					else
					{
						await ImageService.Instance.LoadUrl(image.backgroundPhone).DownloadOnlyAsync();
					}
					await ImageService.Instance.LoadUrl(image.bannerPhone).DownloadOnlyAsync();
					await ImageService.Instance.LoadUrl(image.thumbnail).DownloadOnlyAsync();
				}
				foreach (var i in initiativeView.links) {
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await ImageService.Instance.LoadUrl(i.urlTablet).DownloadOnlyAsync();
					}
					else
					{
						await ImageService.Instance.LoadUrl(i.urlPhone).DownloadOnlyAsync();

					}
				}
				if (GlobalResources.UserAvatar == null)
				{
					await ImageService.Instance.LoadUrl("http://placehold.it/10x10").DownloadOnlyAsync();
				}
				else {
					await ImageService.Instance.LoadUrl(GlobalResources.UserAvatar).DownloadOnlyAsync();
				}
			}
			catch (Exception e) { 
				
			}
		}
	}
}
