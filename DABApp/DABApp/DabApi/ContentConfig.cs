using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DABApp.DabSockets;
using DABApp.Service;
using Newtonsoft.Json;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
	public class Data
	{
		public string updated { get; set; } //RFC3339 date api was updated (use to know when new data has been received)
	}

	public class Nav
	{
		public string title { get; set; } //Title of the item
		public int view { get; set; } //View to use for the item
	}

	public class Blocktext
	{
		public string appInfo { get; set; } //HTML displayed with the "App Information" screen of the app
		public string termsAndConditions { get; set; } //Terms and conditions HTML
		public string resetPassword { get; set; } //HTML displayed under reset password
		public string signUp { get; set; } //HTML displayed on sign up page
		public string login { get; set; } //HTML on login page
		public modeData mode { get; set; }
	}

	public class Versions
	{
		public string version { get; set; }
		public string platform { get; set; }
		public modeData mode { get; set; }
	}

	public class Images
	{
		public string thumbnail { get; set; } //thumbnail url 
		public string bannerPhone { get; set; } //banner graphic on phones
		public string backgroundPhone { get; set; } //background graphic on phones
		public string backgroundTablet { get; set; } //background graphic on tablets
	}

	public class Resource : INotifyPropertyChanged
	{
		public int id { get; set; } //id of the resource
		public string title { get; set; } //title of the resource
		public string description { get; set; } //html description
		public Images images { get; set; } //images used for the resource
		public string feedUrl { get; set; } //url used to get episode data for the resource
		public string type { get; set; } //type of resource - "channel"
		public bool AscendingSort { get; set; } //sorting of the resource
		public EpisodeFilters filter { get; set; } = EpisodeFilters.None; //filters currently used for the resource

		// Available Offline - notifies bound objects of changes */
		private bool _availableOffline = false;
		public bool availableOffline
		{
			get
			{
				return _availableOffline;
			}
			set
			{
				_availableOffline = value;
				OnPropertyChanged("availableOffline");
			}
		}


		private double _IsNotSelected = 1.0;
		public double IsNotSelected
		{
			get
			{
				return _IsNotSelected;
			}
			set
			{
				_IsNotSelected = value;
				OnPropertyChanged("IsNotSelected");
			}
		}

		/* Events to handle Binding */
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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

	/* Available episode filters */
	public enum EpisodeFilters
	{
		None, Favorite, Journal
	}

	/* Banner object */
	public class Banner
	{
		public string type { get; set; } //Either "title" or "content" - title just displays the title of parent view within the banner - content displays content HTML within the banner
		public string urlPhone { get; set; } //Image URL for phones
		public string urlTablet { get; set; } //Image URL for tablets
		public string content { get; set; } //HTML to be displayed within the banner
	}

	/* Link object */
	public class Link
	{
		public string title { get; set; } // title
		public string type { get; set; } //type of link (used for formatting =- "image" or "text"
		public string urlPhone { get; set; } //TODO: Documentation calls this "imageUrl" - verify which it is
		public string urlTablet { get; set; } //TODO: Documentation calls this "imageUrl" - verify which it is
		public string link { get; set; } // URL of the link
		public string linkText { get; set; } //Text for the link (plain text)

		//Boolean that determines if we have a graphic or not
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

		//Boolean that determins if we have text only or not
		//TODO: Simply this and combine with the HasGraphic if possible
		public bool HasNoGraphic
		{
			get
			{
				if (type == "image")
				{
					return false;
				}
				else return true;
			}
		}

		//String of URL to use for the Link based on idiom.
		//TODO: Better naming 
		public string PhoneOrTab
		{
			get
			{
				if (Device.Idiom == TargetIdiom.Tablet)
				{
					return urlTablet;
				}
				else return urlPhone;
			}
		}
	}

	public enum ViewVisibility
	{
		both,
		logged_in

	}

	public class View
	{
		public string type { get; set; } //"content", "app", or "links" 
		public List<Resource> resources { get; set; }
		public int id { get; set; } //id of the view, as the nav will reference it
		public string title { get; set; } //title of the view
		public string deepLink { get; set; }
		public Banner banner { get; set; } //banner information for the view
		public string description { get; set; } //HTML text
		public string content { get; set; } //HTML text
		public List<View> children { get; set; } //array of children content views
		public List<Link> links { get; set; } //aray of links
		public ViewVisibility visible { get; set; } //where the view shoudld be visible
	}

	public class modeData
	{
		public string mode { get; set; }
		public string title { get; set; } //Title of the mode alert
		public string content { get; set; } //STring content of the mode alert
		public List<modeDataButtons> buttons { get; set; } //buttons for the alert
	}

	public class modeDataButtons
	{
		public string key { get; set; } //key of the button (action)
		public string value { get; set; } //text for the button 
	}

	public class ContentConfig
	{
		/* This is a class that has a single instance used throughout the app */

		public static ContentConfig Instance { get; set; }

		static ContentConfig()
		{
			Instance = new ContentConfig();
		}

		//Properties of the instance

		public Data data { get; set; }
		public List<Nav> nav { get; set; }
		public Blocktext blocktext { get; set; }
		public List<View> views { get; set; }
		public List<Versions> versions { get; set; }
		public AppSettings app_settings { get; set; }
		public Options options { get; set; }

		//	public async Task cachImages()
		//	{
		//		var channelView = Instance.views.Single(x => x.title == "Channels");
		//		var initiativeView = Instance.views.Single(x => x.title == "Initiatives");

		//		try
		//		{
		//			foreach (var v in views) {
		//				if (Device.Idiom == TargetIdiom.Tablet)
		//				{
		//					await ImageService.Instance.LoadUrl(v.banner.urlTablet).DownSample().DownloadOnlyAsync();
		//				}
		//				else {
		//					await ImageService.Instance.LoadUrl(v.banner.urlPhone).DownSample().DownloadOnlyAsync();
		//				}
		//			}
		//			foreach (var r in channelView.resources)
		//			{
		//				var image = r.images;
		//				if (Device.Idiom == TargetIdiom.Tablet)
		//				{
		//					await ImageService.Instance.LoadUrl(image.backgroundTablet).DownSample().DownloadOnlyAsync();
		//				}
		//				else
		//				{
		//					await ImageService.Instance.LoadUrl(image.backgroundPhone).DownSample().DownloadOnlyAsync();
		//				}
		//				await ImageService.Instance.LoadUrl(image.bannerPhone).DownSample().DownloadOnlyAsync();
		//				await ImageService.Instance.LoadUrl(image.thumbnail).DownSample().DownloadOnlyAsync();
		//			}
		//			foreach (var i in initiativeView.links) {
		//				if (Device.Idiom == TargetIdiom.Tablet)
		//				{
		//					await ImageService.Instance.LoadUrl(i.urlTablet).DownSample().DownloadOnlyAsync();
		//				}
		//				else
		//				{
		//					await ImageService.Instance.LoadUrl(i.urlPhone).DownSample().DownloadOnlyAsync();

		//				}
		//			}

		//			await ImageService.Instance.LoadUrl(GlobalResources.UserAvatar).DownSample().DownloadOnlyAsync();
		//		}
		//		catch (Exception e) {
		//			Debug.WriteLine($"FFImageLoading Exception caught: {e.Message}");
		//		}
		//	}
		//}

		public class Member
		{
			public string name { get; set; }
			public string role { get; set; }
			public int replyCount { get; set; }
			public int topicCount { get; set; }
		}

		public class Reply
		{
			public int id { get; set; }
			public string content { get; set; }
			public string gmtDate { get; set; }
			public Member member { get; set; }
		}

        public class Topic
        {
            public int id { get; set; }
            public string title { get; set; }
            public string content { get; set; }
            public string lastActivity { get; set; }
            public string replyCount { get; set; }
            public string voiceCount { get; set; }
            public string link { get; set; }
            public Member member { get; set; }
            public List<Reply> replies { get; set; }
        }

        public class PostTopic
		{
			public string title { get; set; }
			public string content { get; set; }
			public int forumId { get; set; }
			public PostTopic(string Title, string Content, int ForumId)
			{
				title = Title;
				content = Content;
				forumId = ForumId;
			}
		}

		public class Forum : INotifyPropertyChanged
		{
			public int id { get; set; }
			public string title { get; set; }
			public string link { get; set; }
			public int topicCount { get; set; }
			private bool _IsBusy;
			public bool IsBusy
			{
				get { return _IsBusy; }
				set
				{
					_IsBusy = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBusy"));
				}
			}
			public ICommand LoadMore { get; set; }
			public ObservableCollection<DabGraphQlTopic> topics { get; set; }

			public event PropertyChangedEventHandler PropertyChanged;

			public Forum()
			{
				int page = 2;
				this.LoadMore = new Command(async () =>
				{
					IsBusy = true;
					var f = await ContentAPI.GetForum();
					page++;
					foreach (var t in f.topics)
					{
						topics.Add(t);
					}
					IsBusy = false;
				});
			}
		}

		public class PostReply
		{
			public string content { get; set; }
			public int topicId { get; set; }
			public PostReply(string Content, int TopicId)
			{
				content = Content;
				topicId = TopicId;
			}
		}

		public class AppSettings
		{
			public string prod_main_link { get; set; } //Production link to the main website (HTTPS://)
			public string prod_give_link { get; set; } //Production link to start giving process (HTTPS://)
			public string prod_journal_link { get; set; } //Production link for journal (WSS://)
			public string prod_feed_link { get; set; } //Production link for feed data (content API HTTPS://)
			public string prod_service_link { get; set; }
			public string stage_main_link { get; set; } //Stage link to the main website (HTTPS://)
			public string stage_give_link { get; set; } //Stage link to start giving process (HTTPS://)
			public string stage_journal_link { get; set; } //Stage link for journal (WSS://)
			public string stage_feed_link { get; set; }//Stage link for feed data (content API HTTPS://)
			public string stage_service_link { get; set; }
		}

		public class Options
		{
			public int token_life { get; set; } = 5;
			public int log_position_interval { get; set; } = 30;
			public int progress_year { get; set; }
			public int new_progress_duration { get; set; }
			public int entire_bible_badge_id { get; set; }
			public int new_testament_badge_id { get; set; }
			public int old_testament_badge_id { get; set; }
			public int episode_year { get; set; }
		}


		/* Information used for routing recording sessions to the right person */
		public class PodcastEmail
		{
			public string Podcast { get; set; }
			public string Email { get; set; }
		}

	}
}
