using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using FFImageLoading;
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

	public class Resource: INotifyPropertyChanged
	{
		public int id { get; set;} //id of the resource
		public string title { get; set; } //title of the resource
		public string description { get; set; } //html description
		public Images images { get; set; } //images used for the resource
		public string feedUrl { get; set; } //url used to get episode data for the resource
		public string type { get; set; } //type of resource - "channel"
        public bool AscendingSort { get; set; } //sorting of the resource
        public EpisodeFilters filter { get; set; } = EpisodeFilters.None; //filters currently used for the resource

        // Available Offline - notifies bound objects of changes */
        private bool _availableOffline = false;
		public bool availableOffline {
            get {
                return _availableOffline;
            }
            set {
                _availableOffline = value;
                OnPropertyChanged("availableOffline");
            }
        }


		private double _IsNotSelected = 1.0;
		public double IsNotSelected { 
			get {
				return _IsNotSelected;
			}
			set {
				_IsNotSelected = value;
				OnPropertyChanged("IsNotSelected");
			}
		}

        /* Events to handle Binding */
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
		public bool HasNoGraphic { 
			get {
				if (type == "image")
				{
					return false;
				}
				else return true;
			}
		}

        //String of URL to use for the Link based on idiom.
        //TODO: Better naming 
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
		public Countries countries { get; set; }

		public async Task cachImages() 
		{ 
			var channelView = Instance.views.Single(x => x.title == "Channels");
			var initiativeView = Instance.views.Single(x => x.title == "Initiatives");

			try
			{
				foreach (var v in views) {
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await ImageService.Instance.LoadUrl(v.banner.urlTablet).DownSample().DownloadOnlyAsync();
					}
					else {
						await ImageService.Instance.LoadUrl(v.banner.urlPhone).DownSample().DownloadOnlyAsync();
					}
				}
				foreach (var r in channelView.resources)
				{
					var image = r.images;
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await ImageService.Instance.LoadUrl(image.backgroundTablet).DownSample().DownloadOnlyAsync();
					}
					else
					{
						await ImageService.Instance.LoadUrl(image.backgroundPhone).DownSample().DownloadOnlyAsync();
					}
					await ImageService.Instance.LoadUrl(image.bannerPhone).DownSample().DownloadOnlyAsync();
					await ImageService.Instance.LoadUrl(image.thumbnail).DownSample().DownloadOnlyAsync();
				}
				foreach (var i in initiativeView.links) {
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await ImageService.Instance.LoadUrl(i.urlTablet).DownSample().DownloadOnlyAsync();
					}
					else
					{
						await ImageService.Instance.LoadUrl(i.urlPhone).DownSample().DownloadOnlyAsync();

					}
				}
				if (GlobalResources.UserAvatar == null)
				{
					await ImageService.Instance.LoadUrl("http://placehold.it/10x10").DownSample().DownloadOnlyAsync();
				}
				else {
					await ImageService.Instance.LoadUrl(GlobalResources.UserAvatar).DownSample().DownloadOnlyAsync();
				}
			}
			catch (Exception e) {
				Debug.WriteLine($"FFImageLoading Exception caught: {e.Message}");
			}
		}
	}

	public class Member
	{
		public string name { get; set; }
		public string avatarUrl { get; set; }
		public string role { get; set; }
		public int replyCount { get; set; }
		public int topicCount { get; set; }
	}

	public class Reply 
	{ 
		public int id { get; set;}
		public string content { get; set;}
		public string gmtDate { get; set;}
		public Member member { get; set;}
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
		public List<Reply> replies { get; set;}
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
        public View view { get; set; }
		public string link { get; set; }
		public int topicCount { get; set; }
        private bool _IsBusy;
        public bool IsBusy {
            get { return _IsBusy; }
            set { _IsBusy = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBusy"));
            }
        }
        public ICommand LoadMore { get; set; }
		public ObservableCollection<Topic> topics { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Forum()
        {
            int page = 2;
            this.LoadMore = new Command(async () =>
            {
                IsBusy = true;
                var f = await ContentAPI.GetForum(view, page);
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
		public string content { get; set;}
		public int topicId { get; set;}
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
		public int progress_year { get; set; } = 2020; 
	}

    public class AO
    {
        public string BGO { get; set; }
        public string BLU { get; set; }
        public string BIE { get; set; }
        public string CAB { get; set; }
        public string CNN { get; set; }
        public string HUA { get; set; }
        public string HUI { get; set; }
        public string CCU { get; set; }
        public string CNO { get; set; }
        public string CUS { get; set; }
        public string LUA { get; set; }
        public string LNO { get; set; }
        public string LSU { get; set; }
        public string MAL { get; set; }
        public string MOX { get; set; }
        public string NAM { get; set; }
        public string UIG { get; set; }
        public string ZAI { get; set; }
    }

    public class AR
    {
        public string C { get; set; }
        public string B { get; set; }
        public string K { get; set; }
        public string H { get; set; }
        public string U { get; set; }
        public string X { get; set; }
        public string W { get; set; }
        public string E { get; set; }
        public string P { get; set; }
        public string Y { get; set; }
        public string L { get; set; }
        public string F { get; set; }
        public string M { get; set; }
        public string N { get; set; }
        public string Q { get; set; }
        public string R { get; set; }
        public string A { get; set; }
        public string J { get; set; }
        public string D { get; set; }
        public string Z { get; set; }
        public string S { get; set; }
        public string G { get; set; }
        public string V { get; set; }
        public string T { get; set; }
    }

    public class AU
    {
        public string ACT { get; set; }
        public string NSW { get; set; }
        public string NT { get; set; }
        public string QLD { get; set; }
        public string SA { get; set; }
        public string TAS { get; set; }
        public string VIC { get; set; }
        public string WA { get; set; }
    }

    public class BO
    {
        public string B { get; set; }
        public string H { get; set; }
        public string C { get; set; }
        public string L { get; set; }
        public string O { get; set; }
        public string N { get; set; }
        public string P { get; set; }
        public string S { get; set; }
        public string T { get; set; }
    }

    public class BR
    {
        public string AC { get; set; }
        public string AL { get; set; }
        public string AP { get; set; }
        public string AM { get; set; }
        public string BA { get; set; }
        public string CE { get; set; }
        public string DF { get; set; }
        public string ES { get; set; }
        public string GO { get; set; }
        public string MA { get; set; }
        public string MT { get; set; }
        public string MS { get; set; }
        public string MG { get; set; }
        public string PA { get; set; }
        public string PB { get; set; }
        public string PR { get; set; }
        public string PE { get; set; }
        public string PI { get; set; }
        public string RJ { get; set; }
        public string RN { get; set; }
        public string RS { get; set; }
        public string RO { get; set; }
        public string RR { get; set; }
        public string SC { get; set; }
        public string SP { get; set; }
        public string SE { get; set; }
        public string TO { get; set; }
    }

    public class CA
    {
        public string AB { get; set; }
        public string BC { get; set; }
        public string MB { get; set; }
        public string NB { get; set; }
        public string NL { get; set; }
        public string NT { get; set; }
        public string NS { get; set; }
        public string NU { get; set; }
        public string ON { get; set; }
        public string PE { get; set; }
        public string QC { get; set; }
        public string SK { get; set; }
        public string YT { get; set; }
    }

    public class CH
    {
        public string AG { get; set; }
        public string AR { get; set; }
        public string AI { get; set; }
        public string BL { get; set; }
        public string BS { get; set; }
        public string BE { get; set; }
        public string FR { get; set; }
        public string GE { get; set; }
        public string GL { get; set; }
        public string GR { get; set; }
        public string JU { get; set; }
        public string LU { get; set; }
        public string NE { get; set; }
        public string NW { get; set; }
        public string OW { get; set; }
        public string SH { get; set; }
        public string SZ { get; set; }
        public string SO { get; set; }
        public string SG { get; set; }
        public string TG { get; set; }
        public string TI { get; set; }
        public string UR { get; set; }
        public string VS { get; set; }
        public string VD { get; set; }
        public string ZG { get; set; }
        public string ZH { get; set; }
    }

    public class CN
    {
        public string CN1 { get; set; }
        public string CN2 { get; set; }
        public string CN3 { get; set; }
        public string CN4 { get; set; }
        public string CN5 { get; set; }
        public string CN6 { get; set; }
        public string CN7 { get; set; }
        public string CN8 { get; set; }
        public string CN9 { get; set; }
        public string CN10 { get; set; }
        public string CN11 { get; set; }
        public string CN12 { get; set; }
        public string CN13 { get; set; }
        public string CN14 { get; set; }
        public string CN15 { get; set; }
        public string CN16 { get; set; }
        public string CN17 { get; set; }
        public string CN18 { get; set; }
        public string CN19 { get; set; }
        public string CN20 { get; set; }
        public string CN21 { get; set; }
        public string CN22 { get; set; }
        public string CN23 { get; set; }
        public string CN24 { get; set; }
        public string CN25 { get; set; }
        public string CN26 { get; set; }
        public string CN27 { get; set; }
        public string CN28 { get; set; }
        public string CN29 { get; set; }
        public string CN30 { get; set; }
        public string CN31 { get; set; }
        public string CN32 { get; set; }
    }

    public class ES
    {
        public string C { get; set; }
        public string VI { get; set; }
        public string AB { get; set; }
        public string A { get; set; }
        public string AL { get; set; }
        public string O { get; set; }
        public string AV { get; set; }
        public string BA { get; set; }
        public string PM { get; set; }
        public string B { get; set; }
        public string BU { get; set; }
        public string CC { get; set; }
        public string CA { get; set; }
        public string S { get; set; }
        public string CS { get; set; }
        public string CE { get; set; }
        public string CR { get; set; }
        public string CO { get; set; }
        public string CU { get; set; }
        public string GI { get; set; }
        public string GR { get; set; }
        public string GU { get; set; }
        public string SS { get; set; }
        public string H { get; set; }
        public string HU { get; set; }
        public string J { get; set; }
        public string LO { get; set; }
        public string GC { get; set; }
        public string LE { get; set; }
        public string L { get; set; }
        public string LU { get; set; }
        public string M { get; set; }
        public string MA { get; set; }
        public string ML { get; set; }
        public string MU { get; set; }
        public string NA { get; set; }
        public string OR { get; set; }
        public string P { get; set; }
        public string PO { get; set; }
        public string SA { get; set; }
        public string TF { get; set; }
        public string SG { get; set; }
        public string SE { get; set; }
        public string SO { get; set; }
        public string T { get; set; }
        public string TE { get; set; }
        public string TO { get; set; }
        public string V { get; set; }
        public string VA { get; set; }
        public string BI { get; set; }
        public string ZA { get; set; }
        public string Z { get; set; }
    }

    public class GR
    {
        public string I { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
        public string F { get; set; }
        public string G { get; set; }
        public string H { get; set; }
        public string J { get; set; }
        public string K { get; set; }
        public string L { get; set; }
        public string M { get; set; }
    }

    

    public class HU
    {
        public string BK { get; set; }
        public string BE { get; set; }
        public string BA { get; set; }
        public string BZ { get; set; }
        public string BU { get; set; }
        public string CS { get; set; }
        public string FE { get; set; }
        public string GS { get; set; }
        public string HB { get; set; }
        public string HE { get; set; }
        public string JN { get; set; }
        public string KE { get; set; }
        public string NO { get; set; }
        public string PE { get; set; }
        public string SO { get; set; }
        public string SZ { get; set; }
        public string TO { get; set; }
        public string VA { get; set; }
        public string VE { get; set; }
        public string ZA { get; set; }
    }

public class ID
{
    public string AC { get; set; }
    public string SU { get; set; }
    public string SB { get; set; }
    public string RI { get; set; }
    public string KR { get; set; }
    public string JA { get; set; }
    public string SS { get; set; }
    public string BB { get; set; }
    public string BE { get; set; }
    public string LA { get; set; }
    public string JK { get; set; }
    public string JB { get; set; }
    public string BT { get; set; }
    public string JT { get; set; }
    public string JI { get; set; }
    public string YO { get; set; }
    public string BA { get; set; }
    public string NB { get; set; }
    public string NT { get; set; }
    public string KB { get; set; }
    public string KT { get; set; }
    public string KI { get; set; }
    public string KS { get; set; }
    public string KU { get; set; }
    public string SA { get; set; }
    public string ST { get; set; }
    public string SG { get; set; }
    public string SR { get; set; }
    public string SN { get; set; }
    public string GO { get; set; }
    public string MA { get; set; }
    public string MU { get; set; }
    public string PA { get; set; }
    public string PB { get; set; }
}

public class IE
{
    public string CW { get; set; }
    public string CN { get; set; }
    public string CE { get; set; }
    public string CO { get; set; }
    public string DL { get; set; }
    public string D { get; set; }
    public string G { get; set; }
    public string KY { get; set; }
    public string KE { get; set; }
    public string KK { get; set; }
    public string LS { get; set; }
    public string LM { get; set; }
    public string LK { get; set; }
    public string LD { get; set; }
    public string LH { get; set; }
    public string MO { get; set; }
    public string MH { get; set; }
    public string MN { get; set; }
    public string OY { get; set; }
    public string RN { get; set; }
    public string SO { get; set; }
    public string TA { get; set; }
    public string WD { get; set; }
    public string WH { get; set; }
    public string WX { get; set; }
    public string WW { get; set; }
}

public class IN
{
    public string AP { get; set; }
    public string AR { get; set; }
    public string AS { get; set; }
    public string BR { get; set; }
    public string CT { get; set; }
    public string GA { get; set; }
    public string GJ { get; set; }
    public string HR { get; set; }
    public string HP { get; set; }
    public string JK { get; set; }
    public string JH { get; set; }
    public string KA { get; set; }
    public string KL { get; set; }
    public string MP { get; set; }
    public string MH { get; set; }
    public string MN { get; set; }
    public string ML { get; set; }
    public string MZ { get; set; }
    public string NL { get; set; }
    public string OR { get; set; }
    public string PB { get; set; }
    public string RJ { get; set; }
    public string SK { get; set; }
    public string TN { get; set; }
    public string TS { get; set; }
    public string TR { get; set; }
    public string UK { get; set; }
    public string UP { get; set; }
    public string WB { get; set; }
    public string AN { get; set; }
    public string CH { get; set; }
    public string DN { get; set; }
    public string DD { get; set; }
    public string DL { get; set; }
    public string LD { get; set; }
    public string PY { get; set; }
}

public class IR
{
    public string KHZ { get; set; }
    public string THR { get; set; }
    public string ILM { get; set; }
    public string BHR { get; set; }
    public string ADL { get; set; }
    public string ESF { get; set; }
    public string YZD { get; set; }
    public string KRH { get; set; }
    public string KRN { get; set; }
    public string HDN { get; set; }
    public string GZN { get; set; }
    public string ZJN { get; set; }
    public string LRS { get; set; }
    public string ABZ { get; set; }
    public string EAZ { get; set; }
    public string WAZ { get; set; }
    public string CHB { get; set; }
    public string SKH { get; set; }
    public string RKH { get; set; }
    public string NKH { get; set; }
    public string SMN { get; set; }
    public string FRS { get; set; }
    public string QHM { get; set; }
    public string KRD { get; set; }
    public string KBD { get; set; }
    public string GLS { get; set; }
    public string GIL { get; set; }
    public string MZN { get; set; }
    public string MKZ { get; set; }
    public string HRZ { get; set; }
    public string SBN { get; set; }
}

public class IT
{
    public string AG { get; set; }
    public string AL { get; set; }
    public string AN { get; set; }
    public string AO { get; set; }
    public string AR { get; set; }
    public string AP { get; set; }
    public string AT { get; set; }
    public string AV { get; set; }
    public string BA { get; set; }
    public string BT { get; set; }
    public string BL { get; set; }
    public string BN { get; set; }
    public string BG { get; set; }
    public string BI { get; set; }
    public string BO { get; set; }
    public string BZ { get; set; }
    public string BS { get; set; }
    public string BR { get; set; }
    public string CA { get; set; }
    public string CL { get; set; }
    public string CB { get; set; }
    public string CE { get; set; }
    public string CT { get; set; }
    public string CZ { get; set; }
    public string CH { get; set; }
    public string CO { get; set; }
    public string CS { get; set; }
    public string CR { get; set; }
    public string KR { get; set; }
    public string CN { get; set; }
    public string EN { get; set; }
    public string FM { get; set; }
    public string FE { get; set; }
    public string FI { get; set; }
    public string FG { get; set; }
    public string FC { get; set; }
    public string FR { get; set; }
    public string GE { get; set; }
    public string GO { get; set; }
    public string GR { get; set; }
    public string IM { get; set; }
    public string IS { get; set; }
    public string SP { get; set; }
    public string AQ { get; set; }
    public string LT { get; set; }
    public string LE { get; set; }
    public string LC { get; set; }
    public string LI { get; set; }
    public string LO { get; set; }
    public string LU { get; set; }
    public string MC { get; set; }
    public string MN { get; set; }
    public string MS { get; set; }
    public string MT { get; set; }
    public string ME { get; set; }
    public string MI { get; set; }
    public string MO { get; set; }
    public string MB { get; set; }
    public string NA { get; set; }
    public string NO { get; set; }
    public string NU { get; set; }
    public string OR { get; set; }
    public string PD { get; set; }
    public string PA { get; set; }
    public string PR { get; set; }
    public string PV { get; set; }
    public string PG { get; set; }
    public string PU { get; set; }
    public string PE { get; set; }
    public string PC { get; set; }
    public string PI { get; set; }
    public string PT { get; set; }
    public string PN { get; set; }
    public string PZ { get; set; }
    public string PO { get; set; }
    public string RG { get; set; }
    public string RA { get; set; }
    public string RC { get; set; }
    public string RE { get; set; }
    public string RI { get; set; }
    public string RN { get; set; }
    public string RM { get; set; }
    public string RO { get; set; }
    public string SA { get; set; }
    public string SS { get; set; }
    public string SV { get; set; }
    public string SI { get; set; }
    public string SR { get; set; }
    public string SO { get; set; }
    public string SU { get; set; }
    public string TA { get; set; }
    public string TE { get; set; }
    public string TR { get; set; }
    public string TO { get; set; }
    public string TP { get; set; }
    public string TN { get; set; }
    public string TV { get; set; }
    public string TS { get; set; }
    public string UD { get; set; }
    public string VA { get; set; }
    public string VE { get; set; }
    public string VB { get; set; }
    public string VC { get; set; }
    public string VR { get; set; }
    public string VV { get; set; }
    public string VI { get; set; }
    public string VT { get; set; }
}

public class JP
{
    public string JP01 { get; set; }
    public string JP02 { get; set; }
    public string JP03 { get; set; }
    public string JP04 { get; set; }
    public string JP05 { get; set; }
    public string JP06 { get; set; }
    public string JP07 { get; set; }
    public string JP08 { get; set; }
    public string JP09 { get; set; }
    public string JP10 { get; set; }
    public string JP11 { get; set; }
    public string JP12 { get; set; }
    public string JP13 { get; set; }
    public string JP14 { get; set; }
    public string JP15 { get; set; }
    public string JP16 { get; set; }
    public string JP17 { get; set; }
    public string JP18 { get; set; }
    public string JP19 { get; set; }
    public string JP20 { get; set; }
    public string JP21 { get; set; }
    public string JP22 { get; set; }
    public string JP23 { get; set; }
    public string JP24 { get; set; }
    public string JP25 { get; set; }
    public string JP26 { get; set; }
    public string JP27 { get; set; }
    public string JP28 { get; set; }
    public string JP29 { get; set; }
    public string JP30 { get; set; }
    public string JP31 { get; set; }
    public string JP32 { get; set; }
    public string JP33 { get; set; }
    public string JP34 { get; set; }
    public string JP35 { get; set; }
    public string JP36 { get; set; }
    public string JP37 { get; set; }
    public string JP38 { get; set; }
    public string JP39 { get; set; }
    public string JP40 { get; set; }
    public string JP41 { get; set; }
    public string JP42 { get; set; }
    public string JP43 { get; set; }
    public string JP44 { get; set; }
    public string JP45 { get; set; }
    public string JP46 { get; set; }
    public string JP47 { get; set; }
}

public class LR
{
    public string BM { get; set; }
    public string BN { get; set; }
    public string GA { get; set; }
    public string GB { get; set; }
    public string GC { get; set; }
    public string GG { get; set; }
    public string GK { get; set; }
    public string LO { get; set; }
    public string MA { get; set; }
    public string MY { get; set; }
    public string MO { get; set; }
    public string NM { get; set; }
    public string RV { get; set; }
    public string RG { get; set; }
    public string SN { get; set; }
}

public class MD
{
    public string C { get; set; }
    public string BL { get; set; }
    public string AN { get; set; }
    public string BS { get; set; }
    public string BR { get; set; }
    public string CH { get; set; }
    public string CT { get; set; }
    public string CL { get; set; }
    public string CS { get; set; }
    public string CM { get; set; }
    public string CR { get; set; }
    public string DN { get; set; }
    public string DR { get; set; }
    public string DB { get; set; }
    public string ED { get; set; }
    public string FL { get; set; }
    public string FR { get; set; }
    public string GE { get; set; }
    public string GL { get; set; }
    public string HN { get; set; }
    public string IL { get; set; }
    public string LV { get; set; }
    public string NS { get; set; }
    public string OC { get; set; }
    public string OR { get; set; }
    public string RZ { get; set; }
    public string RS { get; set; }
    public string SG { get; set; }
    public string SR { get; set; }
    public string ST { get; set; }
    public string SD { get; set; }
    public string SV { get; set; }
    public string TR { get; set; }
    public string TL { get; set; }
    public string UN { get; set; }
}

public class MX
{
    public string DF { get; set; }
    public string JA { get; set; }
    public string NL { get; set; }
    public string AG { get; set; }
    public string BC { get; set; }
    public string BS { get; set; }
    public string CM { get; set; }
    public string CS { get; set; }
    public string CH { get; set; }
    public string CO { get; set; }
    public string CL { get; set; }
    public string DG { get; set; }
    public string GT { get; set; }
    public string GR { get; set; }
    public string HG { get; set; }
    [JsonProperty("MX")]
    public string Mx { get; set; }
    public string MI { get; set; }
    public string MO { get; set; }
    public string NA { get; set; }
    public string OA { get; set; }
    public string PU { get; set; }
    public string QT { get; set; }
    public string QR { get; set; }
    public string SL { get; set; }
    public string SI { get; set; }
    public string SO { get; set; }
    public string TB { get; set; }
    public string TM { get; set; }
    public string TL { get; set; }
    public string VE { get; set; }
    public string YU { get; set; }
    public string ZA { get; set; }
}

public class MY
{
    public string JHR { get; set; }
    public string KDH { get; set; }
    public string KTN { get; set; }
    public string LBN { get; set; }
    public string MLK { get; set; }
    public string NSN { get; set; }
    public string PHG { get; set; }
    public string PNG { get; set; }
    public string PRK { get; set; }
    public string PLS { get; set; }
    public string SBH { get; set; }
    public string SWK { get; set; }
    public string SGR { get; set; }
    public string TRG { get; set; }
    public string PJY { get; set; }
    public string KUL { get; set; }
}

public class NG
{
    public string AB { get; set; }
    public string FC { get; set; }
    public string AD { get; set; }
    public string AK { get; set; }
    public string AN { get; set; }
    public string BA { get; set; }
    public string BY { get; set; }
    public string BE { get; set; }
    public string BO { get; set; }
    public string CR { get; set; }
    public string DE { get; set; }
    public string EB { get; set; }
    public string ED { get; set; }
    public string EK { get; set; }
    public string EN { get; set; }
    public string GO { get; set; }
    public string IM { get; set; }
    public string JI { get; set; }
    public string KD { get; set; }
    public string KN { get; set; }
    public string KT { get; set; }
    public string KE { get; set; }
    public string KO { get; set; }
    public string KW { get; set; }
    public string LA { get; set; }
    public string NA { get; set; }
    public string NI { get; set; }
    public string OG { get; set; }
    public string ON { get; set; }
    public string OS { get; set; }
    public string OY { get; set; }
    public string PL { get; set; }
    public string RI { get; set; }
    public string SO { get; set; }
    public string TA { get; set; }
    public string YO { get; set; }
    public string ZA { get; set; }
}

public class NP
{
    public string BAG { get; set; }
    public string BHE { get; set; }
    public string DHA { get; set; }
    public string GAN { get; set; }
    public string JAN { get; set; }
    public string KAR { get; set; }
    public string KOS { get; set; }
    public string LUM { get; set; }
    public string MAH { get; set; }
    public string MEC { get; set; }
    public string NAR { get; set; }
    public string RAP { get; set; }
    public string SAG { get; set; }
    public string SET { get; set; }
}

public class NZ
{
    public string NL { get; set; }
    public string AK { get; set; }
    public string WA { get; set; }
    public string BP { get; set; }
    public string TK { get; set; }
    public string GI { get; set; }
    public string HB { get; set; }
    public string MW { get; set; }
    public string WE { get; set; }
    public string NS { get; set; }
    public string MB { get; set; }
    public string TM { get; set; }
    public string WC { get; set; }
    public string CT { get; set; }
    public string OT { get; set; }
    public string SL { get; set; }
}

public class PE
{
    public string CAL { get; set; }
    public string LMA { get; set; }
    public string AMA { get; set; }
    public string ANC { get; set; }
    public string APU { get; set; }
    public string ARE { get; set; }
    public string AYA { get; set; }
    public string CAJ { get; set; }
    public string CUS { get; set; }
    public string HUV { get; set; }
    public string HUC { get; set; }
    public string ICA { get; set; }
    public string JUN { get; set; }
    public string LAL { get; set; }
    public string LAM { get; set; }
    public string LIM { get; set; }
    public string LOR { get; set; }
    public string MDD { get; set; }
    public string MOQ { get; set; }
    public string PAS { get; set; }
    public string PIU { get; set; }
    public string PUN { get; set; }
    public string SAM { get; set; }
    public string TAC { get; set; }
    public string TUM { get; set; }
    public string UCA { get; set; }
}

public class PH
{
    public string ABR { get; set; }
    public string AGN { get; set; }
    public string AGS { get; set; }
    public string AKL { get; set; }
    public string ALB { get; set; }
    public string ANT { get; set; }
    public string APA { get; set; }
    public string AUR { get; set; }
    public string BAS { get; set; }
    public string BAN { get; set; }
    public string BTN { get; set; }
    public string BTG { get; set; }
    public string BEN { get; set; }
    public string BIL { get; set; }
    public string BOH { get; set; }
    public string BUK { get; set; }
    public string BUL { get; set; }
    public string CAG { get; set; }
    public string CAN { get; set; }
    public string CAS { get; set; }
    public string CAM { get; set; }
    public string CAP { get; set; }
    public string CAT { get; set; }
    public string CAV { get; set; }
    public string CEB { get; set; }
    public string COM { get; set; }
    public string NCO { get; set; }
    public string DAV { get; set; }
    public string DAS { get; set; }
    public string DAC { get; set; }
    public string DAO { get; set; }
    public string DIN { get; set; }
    public string EAS { get; set; }
    public string GUI { get; set; }
    public string IFU { get; set; }
    public string ILN { get; set; }
    public string ILS { get; set; }
    public string ILI { get; set; }
    public string ISA { get; set; }
    public string KAL { get; set; }
    public string LUN { get; set; }
    public string LAG { get; set; }
    public string LAN { get; set; }
    public string LAS { get; set; }
    public string LEY { get; set; }
    public string MAG { get; set; }
    public string MAD { get; set; }
    public string MAS { get; set; }
    public string MSC { get; set; }
    public string MSR { get; set; }
    public string MOU { get; set; }
    public string NEC { get; set; }
    public string NER { get; set; }
    public string NSA { get; set; }
    public string NUE { get; set; }
    public string NUV { get; set; }
    public string MDC { get; set; }
    public string MDR { get; set; }
    public string PLW { get; set; }
    public string PAM { get; set; }
    public string PAN { get; set; }
    public string QUE { get; set; }
    public string QUI { get; set; }
    public string RIZ { get; set; }
    public string ROM { get; set; }
    public string WSA { get; set; }
    public string SAR { get; set; }
    public string SIQ { get; set; }
    public string SOR { get; set; }
    public string SCO { get; set; }
    public string SLE { get; set; }
    public string SUK { get; set; }
    public string SLU { get; set; }
    public string SUN { get; set; }
    public string SUR { get; set; }
    public string TAR { get; set; }
    public string TAW { get; set; }
    public string ZMB { get; set; }
    public string ZAN { get; set; }
    public string ZAS { get; set; }
    public string ZSI { get; set; }
    [JsonProperty("00")]
    public string ZeroZero { get; set; }
    }

    public class PK
    {
        public string JK { get; set; }
        public string BA { get; set; }
        public string TA { get; set; }
        public string GB { get; set; }
        public string IS { get; set; }
        public string KP { get; set; }
        public string PB { get; set; }
        public string SD { get; set; }
    }

    public class RO
    {
        public string AB { get; set; }
        public string AR { get; set; }
        public string AG { get; set; }
        public string BC { get; set; }
        public string BH { get; set; }
        public string BN { get; set; }
        public string BT { get; set; }
        public string BR { get; set; }
        public string BV { get; set; }
        public string B { get; set; }
        public string BZ { get; set; }
        public string CL { get; set; }
        public string CS { get; set; }
        public string CJ { get; set; }
        public string CT { get; set; }
        public string CV { get; set; }
        public string DB { get; set; }
        public string DJ { get; set; }
        public string GL { get; set; }
        public string GR { get; set; }
        public string GJ { get; set; }
        public string HR { get; set; }
        public string HD { get; set; }
        public string IL { get; set; }
        public string IS { get; set; }
        public string IF { get; set; }
        public string MM { get; set; }
        public string MH { get; set; }
        public string MS { get; set; }
        public string NT { get; set; }
        public string OT { get; set; }
        public string PH { get; set; }
        public string SJ { get; set; }
        public string SM { get; set; }
        public string SB { get; set; }
        public string SV { get; set; }
        public string TR { get; set; }
        public string TM { get; set; }
        public string TL { get; set; }
        public string VL { get; set; }
        public string VS { get; set; }
        public string VN { get; set; }
    }

    public class TR
    {
        public string TR01 { get; set; }
        public string TR02 { get; set; }
        public string TR03 { get; set; }
        public string TR04 { get; set; }
        public string TR05 { get; set; }
        public string TR06 { get; set; }
        public string TR07 { get; set; }
        public string TR08 { get; set; }
        public string TR09 { get; set; }
        public string TR10 { get; set; }
        public string TR11 { get; set; }
        public string TR12 { get; set; }
        public string TR13 { get; set; }
        public string TR14 { get; set; }
        public string TR15 { get; set; }
        public string TR16 { get; set; }
        public string TR17 { get; set; }
        public string TR18 { get; set; }
        public string TR19 { get; set; }
        public string TR20 { get; set; }
        public string TR21 { get; set; }
        public string TR22 { get; set; }
        public string TR23 { get; set; }
        public string TR24 { get; set; }
        public string TR25 { get; set; }
        public string TR26 { get; set; }
        public string TR27 { get; set; }
        public string TR28 { get; set; }
        public string TR29 { get; set; }
        public string TR30 { get; set; }
        public string TR31 { get; set; }
        public string TR32 { get; set; }
        public string TR33 { get; set; }
        public string TR34 { get; set; }
        public string TR35 { get; set; }
        public string TR36 { get; set; }
        public string TR37 { get; set; }
        public string TR38 { get; set; }
        public string TR39 { get; set; }
        public string TR40 { get; set; }
        public string TR41 { get; set; }
        public string TR42 { get; set; }
        public string TR43 { get; set; }
        public string TR44 { get; set; }
        public string TR45 { get; set; }
        public string TR46 { get; set; }
        public string TR47 { get; set; }
        public string TR48 { get; set; }
        public string TR49 { get; set; }
        public string TR50 { get; set; }
        public string TR51 { get; set; }
        public string TR52 { get; set; }
        public string TR53 { get; set; }
        public string TR54 { get; set; }
        public string TR55 { get; set; }
        public string TR56 { get; set; }
        public string TR57 { get; set; }
        public string TR58 { get; set; }
        public string TR59 { get; set; }
        public string TR60 { get; set; }
        public string TR61 { get; set; }
        public string TR62 { get; set; }
        public string TR63 { get; set; }
        public string TR64 { get; set; }
        public string TR65 { get; set; }
        public string TR66 { get; set; }
        public string TR67 { get; set; }
        public string TR68 { get; set; }
        public string TR69 { get; set; }
        public string TR70 { get; set; }
        public string TR71 { get; set; }
        public string TR72 { get; set; }
        public string TR73 { get; set; }
        public string TR74 { get; set; }
        public string TR75 { get; set; }
        public string TR76 { get; set; }
        public string TR77 { get; set; }
        public string TR78 { get; set; }
        public string TR79 { get; set; }
        public string TR80 { get; set; }
        public string TR81 { get; set; }
    }

    public class TZ
    {
        public string TZ01 { get; set; }
        public string TZ02 { get; set; }
        public string TZ03 { get; set; }
        public string TZ04 { get; set; }
        public string TZ05 { get; set; }
        public string TZ06 { get; set; }
        public string TZ07 { get; set; }
        public string TZ08 { get; set; }
        public string TZ09 { get; set; }
        public string TZ10 { get; set; }
        public string TZ11 { get; set; }
        public string TZ12 { get; set; }
        public string TZ13 { get; set; }
        public string TZ14 { get; set; }
        public string TZ15 { get; set; }
        public string TZ16 { get; set; }
        public string TZ17 { get; set; }
        public string TZ18 { get; set; }
        public string TZ19 { get; set; }
        public string TZ20 { get; set; }
        public string TZ21 { get; set; }
        public string TZ22 { get; set; }
        public string TZ23 { get; set; }
        public string TZ24 { get; set; }
        public string TZ25 { get; set; }
        public string TZ26 { get; set; }
        public string TZ27 { get; set; }
        public string TZ28 { get; set; }
        public string TZ29 { get; set; }
        public string TZ30 { get; set; }
    }

    public class US
    {
        public string AL { get; set; }
        public string AK { get; set; }
        public string AZ { get; set; }
        public string AR { get; set; }
        public string CA { get; set; }
        public string CO { get; set; }
        public string CT { get; set; }
        public string DE { get; set; }
        public string DC { get; set; }
        public string FL { get; set; }
        public string GA { get; set; }
        public string HI { get; set; }
        public string ID { get; set; }
        public string IL { get; set; }
        public string IN { get; set; }
        public string IA { get; set; }
        public string KS { get; set; }
        public string KY { get; set; }
        public string LA { get; set; }
        public string ME { get; set; }
        public string MD { get; set; }
        public string MA { get; set; }
        public string MI { get; set; }
        public string MN { get; set; }
        public string MS { get; set; }
        public string MO { get; set; }
        public string MT { get; set; }
        public string NE { get; set; }
        public string NV { get; set; }
        public string NH { get; set; }
        public string NJ { get; set; }
        public string NM { get; set; }
        public string NY { get; set; }
        public string NC { get; set; }
        public string ND { get; set; }
        public string OH { get; set; }
        public string OK { get; set; }
        public string OR { get; set; }
        public string PA { get; set; }
        public string RI { get; set; }
        public string SC { get; set; }
        public string SD { get; set; }
        public string TN { get; set; }
        public string TX { get; set; }
        public string UT { get; set; }
        public string VT { get; set; }
        public string VA { get; set; }
        public string WA { get; set; }
        public string WV { get; set; }
        public string WI { get; set; }
        public string WY { get; set; }
        public string AA { get; set; }
        public string AE { get; set; }
        public string AP { get; set; }
    }

    public class ZA
    {
        public string EC { get; set; }
        public string FS { get; set; }
        public string GP { get; set; }
        public string KZN { get; set; }
        public string LP { get; set; }
        public string MP { get; set; }
        public string NC { get; set; }
        public string NW { get; set; }
        public string WC { get; set; }
    }

    public class BG
    {
        [JsonProperty("BD-01")]
        public string BG_01 { get; set; }
        [JsonProperty("BD-02")]
        public string BG_02 { get; set; }
        [JsonProperty("BD-08")]
        public string BG_08 { get; set; }
        [JsonProperty("BD-07")]
        public string BG_07 { get; set; }
        [JsonProperty("BD-26")]
        public string BG_26 { get; set; }
        [JsonProperty("BD-09")]
        public string BG_09 { get; set; }
        [JsonProperty("BD-10")]
        public string BG_10 { get; set; }
        [JsonProperty("BD-11")]
        public string BG_11 { get; set; }
        [JsonProperty("BD-12")]
        public string BG_12 { get; set; }
        [JsonProperty("BD-13")]
        public string BG_13 { get; set; }
        [JsonProperty("BD-14")]
        public string BG_14 { get; set; }
        [JsonProperty("BD-15")]
        public string BG_15 { get; set; }
        [JsonProperty("BD-16")]
        public string BG_16 { get; set; }
        [JsonProperty("BD-17")]
        public string BG_17 { get; set; }
        [JsonProperty("BD-18")]
        public string BG_18 { get; set; }
        [JsonProperty("BD-27")]
        public string BG_27 { get; set; }
        [JsonProperty("BD-19")]
        public string BG_19 { get; set; }
        [JsonProperty("BD-20")]
        public string BG_20 { get; set; }
        [JsonProperty("BD-21")]
        public string BG_21 { get; set; }
        [JsonProperty("BD-23")]
        public string BG_23 { get; set; }
        [JsonProperty("BD-22")]
        public string BG_22 { get; set; }
        [JsonProperty("BD-24")]
        public string BG_24 { get; set; }
        [JsonProperty("BD-25")]
        public string BG_25 { get; set; }
        [JsonProperty("BD-03")]
        public string BG_03 { get; set; }
        [JsonProperty("BD-04")]
        public string BG_04 { get; set; }
        [JsonProperty("BD-05")]
        public string BG_05 { get; set; }
        [JsonProperty("BD-06")]
        public string BG_06 { get; set; }
        [JsonProperty("BD-28")]
        public string BG_28 { get; set; }
    }

    public class PY
    {
        [JsonProperty("PY-ASU")]
        public string PY_ASU { get; set; }
        [JsonProperty("PY-1")]
        public string PY_1 { get; set; }
        [JsonProperty("PY-2")]
        public string PY_2 { get; set; }
        [JsonProperty("PY-3")]
        public string PY_3 { get; set; }
        [JsonProperty("PY-4")]
        public string PY_4 { get; set; }
        [JsonProperty("PY-5")]
        public string PY_5 { get; set; }
        [JsonProperty("PY-6")]
        public string PY_6 { get; set; }
        [JsonProperty("PY-7")]
        public string PY_7 { get; set; }
        [JsonProperty("PY-8")]
        public string PY_8 { get; set; }
        [JsonProperty("PY-9")]
        public string PY_9 { get; set; }
        [JsonProperty("PY-10")]
        public string PY_10 { get; set; }
        [JsonProperty("PY-11")]
        public string PY_11 { get; set; }
        [JsonProperty("PY-12")]
        public string PY_12 { get; set; }
        [JsonProperty("PY-13")]
        public string PY_13 { get; set; }
        [JsonProperty("PY-14")]
        public string PY_14 { get; set; }
        [JsonProperty("PY-15")]
        public string PY_15 { get; set; }
        [JsonProperty("PY-16")]
        public string PY_16 { get; set; }
        [JsonProperty("PY-17")]
        public string PY_17 { get; set; }
    }

    public class HK
    {
        [JsonProperty("HONG KONG")]
        public string HONGKONG { get; set; }
        public string KOWLOON { get; set; }
        [JsonProperty("NEW TERRITORIES")]
        public string NEWTERRITORIES { get; set; }
    }

    public class BD
    {
        [JsonProperty("BD-05")]
        public string BD_05 { get; set; }
        [JsonProperty("BD-01")]
        public string BD_01 { get; set; }
        [JsonProperty("BD-02")]
        public string BD_02 { get; set; }
        [JsonProperty("BD-06")]
        public string BD_06 { get; set; }
        [JsonProperty("BD-07")]
        public string BD_07 { get; set; }
        [JsonProperty("BD-03")]
        public string BD_03 { get; set; }
        [JsonProperty("BD-04")]
        public string BD_04 { get; set; }
        [JsonProperty("BD-09")]
        public string BD_09 { get; set; }
        [JsonProperty("BD-10")]
        public string BD_10 { get; set; }
        [JsonProperty("BD-12")]
        public string BD_12 { get; set; }
        [JsonProperty("BD-11")]
        public string BD_11 { get; set; }
        [JsonProperty("BD-08")]
        public string BD_08 { get; set; }
        [JsonProperty("BD-13")]
        public string BD_13 { get; set; }
        [JsonProperty("BD-14")]
        public string BD_14 { get; set; }
        [JsonProperty("BD-15")]
        public string BD_15 { get; set; }
        [JsonProperty("BD-16")]
        public string BD_16 { get; set; }
        [JsonProperty("BD-19")]
        public string BD_19 { get; set; }
        [JsonProperty("BD-18")]
        public string BD_18 { get; set; }
        [JsonProperty("BD-17")]
        public string BD_17 { get; set; }
        [JsonProperty("BD-20")]
        public string BD_20 { get; set; }
        [JsonProperty("BD-21")]
        public string BD_21 { get; set; }
        [JsonProperty("BD-22")]
        public string BD_22 { get; set; }
        [JsonProperty("BD-25")]
        public string BD_25 { get; set; }
        [JsonProperty("BD-23")]
        public string BD_23 { get; set; }
        [JsonProperty("BD-24")]
        public string BD_24 { get; set; }
        [JsonProperty("BD-29")]
        public string BD_29 { get; set; }
        [JsonProperty("BD-27")]
        public string BD_27 { get; set; }
        [JsonProperty("BD-26")]
        public string BD_26 { get; set; }
        [JsonProperty("BD-28")]
        public string BD_28 { get; set; }
        [JsonProperty("BD-30")]
        public string BD_30 { get; set; }
        [JsonProperty("BD-31")]
        public string BD_31 { get; set; }
        [JsonProperty("BD-32")]
        public string BD_32 { get; set; }
        [JsonProperty("BD-36")]
        public string BD_36 { get; set; }
        [JsonProperty("BD-37")]
        public string BD_37 { get; set; }
        [JsonProperty("BD-33")]
        public string BD_33 { get; set; }
        [JsonProperty("BD-39")]
        public string BD_39 { get; set; }
        [JsonProperty("BD-38")]
        public string BD_38 { get; set; }
        [JsonProperty("BD-35")]
        public string BD_35 { get; set; }
        [JsonProperty("BD-34")]
        public string BD_34 { get; set; }
        [JsonProperty("BD-48")]
        public string BD_48 { get; set; }
        [JsonProperty("BD-43")]
        public string BD_43 { get; set; }
        [JsonProperty("BD-40")]
        public string BD_40 { get; set; }
        [JsonProperty("BD-42")]
        public string BD_42 { get; set; }
        [JsonProperty("BD-44")]
        public string BD_44 { get; set; }
        [JsonProperty("BD-45")]
        public string BD_45 { get; set; }
        [JsonProperty("BD-41")]
        public string BD_41 { get; set; }
        [JsonProperty("BD-46")]
        public string BD_46 { get; set; }
        [JsonProperty("BD-47")]
        public string BD_47 { get; set; }
        [JsonProperty("BD-49")]
        public string BD_49 { get; set; }
        [JsonProperty("BD-52")]
        public string BD_52 { get; set; }
        [JsonProperty("BD-51")]
        public string BD_51 { get; set; }
        [JsonProperty("BD-50")]
        public string BD_50 { get; set; }
        [JsonProperty("BD-53")]
        public string BD_53 { get; set; }
        [JsonProperty("BD-54")]
        public string BD_54 { get; set; }
        [JsonProperty("BD-56")]
        public string BD_56 { get; set; }
        [JsonProperty("BD-55")]
        public string BD_55 { get; set; }
        [JsonProperty("BD-58")]
        public string BD_58 { get; set; }
        [JsonProperty("BD-62")]
        public string BD_62 { get; set; }
        [JsonProperty("BD-57")]
        public string BD_57 { get; set; }
        [JsonProperty("BD-59")]
        public string BD_59 { get; set; }
        [JsonProperty("BD-61")]
        public string BD_61 { get; set; }
        [JsonProperty("BD-60")]
        public string BD_60 { get; set; }
        [JsonProperty("BD-63")]
        public string BD_63 { get; set; }
        [JsonProperty("BD-64")]
        public string BD_64 { get; set; }
    }

    public class TH
    {
        [JsonProperty("TH-37")]
        public string TH_37 { get; set; }
        [JsonProperty("TH-15")]
        public string TH_15 { get; set; }
        [JsonProperty("TH-14")]
        public string TH_14 { get; set; }
        [JsonProperty("TH-10")]
        public string TH_10 { get; set; }
        [JsonProperty("TH-38")]
        public string TH_38 { get; set; }
        [JsonProperty("TH-31")]
        public string TH_31 { get; set; }
        [JsonProperty("TH-24")]
        public string TH_24 { get; set; }
        [JsonProperty("TH-18")]
        public string TH_18 { get; set; }
        [JsonProperty("TH-36")]
        public string TH_36 { get; set; }
        [JsonProperty("TH-22")]
        public string TH_22 { get; set; }
        [JsonProperty("TH-50")]
        public string TH_50 { get; set; }
        [JsonProperty("TH-57")]
        public string TH_57 { get; set; }
        [JsonProperty("TH-20")]
        public string TH_20 { get; set; }
        [JsonProperty("TH-86")]
        public string TH_86 { get; set; }
        [JsonProperty("TH-46")]
        public string TH_46 { get; set; }
        [JsonProperty("TH-62")]
        public string TH_62 { get; set; }
        [JsonProperty("TH-71")]
        public string TH_71 { get; set; }
        [JsonProperty("TH-40")]
        public string TH_40 { get; set; }
        [JsonProperty("TH-81")]
        public string TH_81 { get; set; }
        [JsonProperty("TH-52")]
        public string TH_52 { get; set; }
        [JsonProperty("TH-51")]
        public string TH_51 { get; set; }
        [JsonProperty("TH-42")]
        public string TH_42 { get; set; }
        [JsonProperty("TH-16")]
        public string TH_16 { get; set; }
        [JsonProperty("TH-58")]
        public string TH_58 { get; set; }
        [JsonProperty("TH-44")]
        public string TH_44 { get; set; }
        [JsonProperty("TH-49")]
        public string TH_49 { get; set; }
        [JsonProperty("TH-26")]
        public string TH_26 { get; set; }
        [JsonProperty("TH-73")]
        public string TH_73 { get; set; }
        [JsonProperty("TH-48")]
        public string TH_48 { get; set; }
        [JsonProperty("TH-30")]
        public string TH_30 { get; set; }
        [JsonProperty("TH-60")]
        public string TH_60 { get; set; }
        [JsonProperty("TH-80")]
        public string TH_80 { get; set; }
        [JsonProperty("TH-55")]
        public string TH_55 { get; set; }
        [JsonProperty("TH-96")]
        public string TH_96 { get; set; }
        [JsonProperty("TH-39")]
        public string TH_39 { get; set; }
        [JsonProperty("TH-43")]
        public string TH_43 { get; set; }
        [JsonProperty("TH-12")]
        public string TH_12 { get; set; }
        [JsonProperty("TH-13")]
        public string TH_13 { get; set; }
        [JsonProperty("TH-94")]
        public string TH_94 { get; set; }
        [JsonProperty("TH-82")]
        public string TH_82 { get; set; }
        [JsonProperty("TH-93")]
        public string TH_93 { get; set; }
        [JsonProperty("TH-56")]
        public string TH_56 { get; set; }
        [JsonProperty("TH-67")]
        public string TH_67 { get; set; }
        [JsonProperty("TH-76")]
        public string TH_76 { get; set; }
        [JsonProperty("TH-66")]
        public string TH_66 { get; set; }
        [JsonProperty("TH-65")]
        public string TH_65 { get; set; }
        [JsonProperty("TH-54")]
        public string TH_54 { get; set; }
        [JsonProperty("TH-83")]
        public string TH_83 { get; set; }
        [JsonProperty("TH-25")]
        public string TH_25 { get; set; }
        [JsonProperty("TH-77")]
        public string TH_77 { get; set; }
        [JsonProperty("TH-85")]
        public string TH_85 { get; set; }
        [JsonProperty("TH-70")]
        public string TH_70 { get; set; }
        [JsonProperty("TH-21")]
        public string TH_21 { get; set; }
        [JsonProperty("TH-45")]
        public string TH_45 { get; set; }
        [JsonProperty("TH-27")]
        public string TH_27 { get; set; }
        [JsonProperty("TH-47")]
        public string TH_47 { get; set; }
        [JsonProperty("TH-11")]
        public string TH_11 { get; set; }
        [JsonProperty("TH-74")]
        public string TH_74 { get; set; }
        [JsonProperty("TH-75")]
        public string TH_75 { get; set; }
        [JsonProperty("TH-19")]
        public string TH_19 { get; set; }
        [JsonProperty("TH-91")]
        public string TH_91 { get; set; }
        [JsonProperty("TH-17")]
        public string TH_17 { get; set; }
        [JsonProperty("TH-33")]
        public string TH_33 { get; set; }
        [JsonProperty("TH-90")]
        public string TH_90 { get; set; }
        [JsonProperty("TH-64")]
        public string TH_64 { get; set; }
        [JsonProperty("TH-72")]
        public string TH_72 { get; set; }
        [JsonProperty("TH-84")]
        public string TH_84 { get; set; }
        [JsonProperty("TH-32")]
        public string TH_32 { get; set; }
        [JsonProperty("TH-63")]
        public string TH_63 { get; set; }
        [JsonProperty("TH-92")]
        public string TH_92 { get; set; }
        [JsonProperty("TH-23")]
        public string TH_23 { get; set; }
        [JsonProperty("TH-34")]
        public string TH_34 { get; set; }
        [JsonProperty("TH-41")]
        public string TH_41 { get; set; }
        [JsonProperty("TH-61")]
        public string TH_61 { get; set; }
        [JsonProperty("TH-53")]
        public string TH_53 { get; set; }
        [JsonProperty("TH-95")]
        public string TH_95 { get; set; }
        [JsonProperty("TH-35")]
        public string TH_35 { get; set; }
    }

    public class Countries
	{
		public Names names { get; set; }
		public Labels labels { get; set; }
		public States states { get; set; }
	}

    public class States
    {
        public object AF { get; set; }
        public AO AO { get; set; }
        public AR AR { get; set; }
        public object AT { get; set; }
        public AU AU { get; set; }
        public object AX { get; set; }
        public BD BD { get; set; }
        public object BE { get; set; }
        public BG BG { get; set; }
        public object BH { get; set; }
        public object BI { get; set; }
        public BO BO { get; set; }
        public BR BR { get; set; }
        public CA CA { get; set; }
        public CH CH { get; set; }
        public CN CN { get; set; }
        public object CZ { get; set; }
        public object DE { get; set; }
        public object DK { get; set; }
        public object EE { get; set; }
        public ES ES { get; set; }
        public object FI { get; set; }
        public object FR { get; set; }
        public object GP { get; set; }
        public GR GR { get; set; }
        public object GF { get; set; }
        public HK HK { get; set; }
        public HU HU { get; set; }
        public ID ID { get; set; }
        public IE IE { get; set; }
        public IN IN { get; set; }
        public IR IR { get; set; }
        public object IS { get; set; }
        public IT IT { get; set; }
        public object IL { get; set; }
        public object IM { get; set; }
        public JP JP { get; set; }
        public object KR { get; set; }
        public object KW { get; set; }
        public object LB { get; set; }
        public LR LR { get; set; }
        public object LU { get; set; }
        public MD MD { get; set; }
        public object MQ { get; set; }
        public object MT { get; set; }
        public MX MX { get; set; }
        public MY MY { get; set; }
        public NG NG { get; set; }
        public object NL { get; set; }
        public object NO { get; set; }
        public NP NP { get; set; }
        public NZ NZ { get; set; }
        public PE PE { get; set; }
        public PH PH { get; set; }
        public PK PK { get; set; }
        public object PL { get; set; }
        public object PT { get; set; }
        public PY PY { get; set; }
        public object RE { get; set; }
        public RO RO { get; set; }
        public object RS { get; set; }
        public object SG { get; set; }
        public object SK { get; set; }
        public object SI { get; set; }
        public TH TH { get; set; }
        public TR TR { get; set; }
        public TZ TZ { get; set; }
        public object LK { get; set; }
        public object SE { get; set; }
        public US US { get; set; }
        public object VN { get; set; }
        public object YT { get; set; }
        public ZA ZA { get; set; }
    }


    public class Names
    {
        public string AF { get; set; }
        public string AX { get; set; }
        public string AL { get; set; }
        public string DZ { get; set; }
        public string AS { get; set; }
        public string AD { get; set; }
        public string AO { get; set; }
        public string AI { get; set; }
        public string AQ { get; set; }
        public string AG { get; set; }
        public string AR { get; set; }
        public string AM { get; set; }
        public string AW { get; set; }
        public string AU { get; set; }
        public string AT { get; set; }
        public string AZ { get; set; }
        public string BS { get; set; }
        public string BH { get; set; }
        public string BD { get; set; }
        public string BB { get; set; }
        public string BY { get; set; }
        public string BE { get; set; }
        public string PW { get; set; }
        public string BZ { get; set; }
        public string BJ { get; set; }
        public string BM { get; set; }
        public string BT { get; set; }
        public string BO { get; set; }
        public string BQ { get; set; }
        public string BA { get; set; }
        public string BW { get; set; }
        public string BV { get; set; }
        public string BR { get; set; }
        public string IO { get; set; }
        public string BN { get; set; }
        public string BG { get; set; }
        public string BF { get; set; }
        public string BI { get; set; }
        public string KH { get; set; }
        public string CM { get; set; }
        public string CA { get; set; }
        public string CV { get; set; }
        public string KY { get; set; }
        public string CF { get; set; }
        public string TD { get; set; }
        public string CL { get; set; }
        public string CN { get; set; }
        public string CX { get; set; }
        public string CC { get; set; }
        public string CO { get; set; }
        public string KM { get; set; }
        public string CG { get; set; }
        public string CD { get; set; }
        public string CK { get; set; }
        public string CR { get; set; }
        public string HR { get; set; }
        public string CU { get; set; }
        public string CW { get; set; }
        public string CY { get; set; }
        public string CZ { get; set; }
        public string DK { get; set; }
        public string DJ { get; set; }
        public string DM { get; set; }
        public string DO { get; set; }
        public string EC { get; set; }
        public string EG { get; set; }
        public string SV { get; set; }
        public string GQ { get; set; }
        public string ER { get; set; }
        public string EE { get; set; }
        public string ET { get; set; }
        public string FK { get; set; }
        public string FO { get; set; }
        public string FJ { get; set; }
        public string FI { get; set; }
        public string FR { get; set; }
        public string GF { get; set; }
        public string PF { get; set; }
        public string TF { get; set; }
        public string GA { get; set; }
        public string GM { get; set; }
        public string GE { get; set; }
        public string DE { get; set; }
        public string GH { get; set; }
        public string GI { get; set; }
        public string GR { get; set; }
        public string GL { get; set; }
        public string GD { get; set; }
        public string GP { get; set; }
        public string GU { get; set; }
        public string GT { get; set; }
        public string GG { get; set; }
        public string GN { get; set; }
        public string GW { get; set; }
        public string GY { get; set; }
        public string HT { get; set; }
        public string HM { get; set; }
        public string HN { get; set; }
        public string HK { get; set; }
        public string HU { get; set; }
        public string IS { get; set; }
        public string IN { get; set; }
        public string ID { get; set; }
        public string IR { get; set; }
        public string IQ { get; set; }
        public string IE { get; set; }
        public string IM { get; set; }
        public string IL { get; set; }
        public string IT { get; set; }
        public string CI { get; set; }
        public string JM { get; set; }
        public string JP { get; set; }
        public string JE { get; set; }
        public string JO { get; set; }
        public string KZ { get; set; }
        public string KE { get; set; }
        public string KI { get; set; }
        public string KW { get; set; }
        public string KG { get; set; }
        public string LA { get; set; }
        public string LV { get; set; }
        public string LB { get; set; }
        public string LS { get; set; }
        public string LR { get; set; }
        public string LY { get; set; }
        public string LI { get; set; }
        public string LT { get; set; }
        public string LU { get; set; }
        public string MO { get; set; }
        public string MK { get; set; }
        public string MG { get; set; }
        public string MW { get; set; }
        public string MY { get; set; }
        public string MV { get; set; }
        public string ML { get; set; }
        public string MT { get; set; }
        public string MH { get; set; }
        public string MQ { get; set; }
        public string MR { get; set; }
        public string MU { get; set; }
        public string YT { get; set; }
        public string MX { get; set; }
        public string FM { get; set; }
        public string MD { get; set; }
        public string MC { get; set; }
        public string MN { get; set; }
        public string ME { get; set; }
        public string MS { get; set; }
        public string MA { get; set; }
        public string MZ { get; set; }
        public string MM { get; set; }
        public string NA { get; set; }
        public string NR { get; set; }
        public string NP { get; set; }
        public string NL { get; set; }
        public string NC { get; set; }
        public string NZ { get; set; }
        public string NI { get; set; }
        public string NE { get; set; }
        public string NG { get; set; }
        public string NU { get; set; }
        public string NF { get; set; }
        public string MP { get; set; }
        public string KP { get; set; }
        public string NO { get; set; }
        public string OM { get; set; }
        public string PK { get; set; }
        public string PS { get; set; }
        public string PA { get; set; }
        public string PG { get; set; }
        public string PY { get; set; }
        public string PE { get; set; }
        public string PH { get; set; }
        public string PN { get; set; }
        public string PL { get; set; }
        public string PT { get; set; }
        public string PR { get; set; }
        public string QA { get; set; }
        public string RE { get; set; }
        public string RO { get; set; }
        public string RU { get; set; }
        public string RW { get; set; }
        public string BL { get; set; }
        public string SH { get; set; }
        public string KN { get; set; }
        public string LC { get; set; }
        public string MF { get; set; }
        public string SX { get; set; }
        public string PM { get; set; }
        public string VC { get; set; }
        public string SM { get; set; }
        public string ST { get; set; }
        public string SA { get; set; }
        public string SN { get; set; }
        public string RS { get; set; }
        public string SC { get; set; }
        public string SL { get; set; }
        public string SG { get; set; }
        public string SK { get; set; }
        public string SI { get; set; }
        public string SB { get; set; }
        public string SO { get; set; }
        public string ZA { get; set; }
        public string GS { get; set; }
        public string KR { get; set; }
        public string SS { get; set; }
        public string ES { get; set; }
        public string LK { get; set; }
        public string SD { get; set; }
        public string SR { get; set; }
        public string SJ { get; set; }
        public string SZ { get; set; }
        public string SE { get; set; }
        public string CH { get; set; }
        public string SY { get; set; }
        public string TW { get; set; }
        public string TJ { get; set; }
        public string TZ { get; set; }
        public string TH { get; set; }
        public string TL { get; set; }
        public string TG { get; set; }
        public string TK { get; set; }
        public string TO { get; set; }
        public string TT { get; set; }
        public string TN { get; set; }
        public string TR { get; set; }
        public string TM { get; set; }
        public string TC { get; set; }
        public string TV { get; set; }
        public string UG { get; set; }
        public string UA { get; set; }
        public string AE { get; set; }
        public string GB { get; set; }
        public string US { get; set; }
        public string UM { get; set; }
        public string UY { get; set; }
        public string UZ { get; set; }
        public string VU { get; set; }
        public string VA { get; set; }
        public string VE { get; set; }
        public string VN { get; set; }
        public string VG { get; set; }
        public string VI { get; set; }
        public string WF { get; set; }
        public string EH { get; set; }
        public string WS { get; set; }
        public string YE { get; set; }
        public string ZM { get; set; }
        public string ZW { get; set; }
    }

    public class Labels
	{
		public string AO { get; set; }
		public string AU { get; set; }
		public string BD { get; set; }
		public string BE { get; set; }
		public string CA { get; set; }
		public string CH { get; set; }
		public string CL { get; set; }
		public string CN { get; set; }
		public string HK { get; set; }
		public string HU { get; set; }
		public string ID { get; set; }
		public string IE { get; set; }
		public string IT { get; set; }
		public string JP { get; set; }
		public string LV { get; set; }
		public string MZ { get; set; }
		public string NL { get; set; }
		public string NG { get; set; }
		public string NZ { get; set; }
		public string NP { get; set; }
		public string RO { get; set; }
		public string ES { get; set; }
		public string LI { get; set; }
		public string MD { get; set; }
		public string TR { get; set; }
		public string UG { get; set; }
		public string US { get; set; }
		public string GB { get; set; }
		public string ST { get; set; }
		public string ZA { get; set; }
	}

	/* Converter class to convert short month names to long names */
	public static class MonthConverter
    {
        public static string ConvertToFull(string ShortHand)
        {
            switch (ShortHand)
            {
                case ("Jan"):
                    return "January";
                case ("Feb"):
                    return "February";
                case ("Mar"):
                    return "March";
                case ("Apr"):
                    return "April";
                case ("Jun"):
                    return "June";
                case ("Jul"):
                    return "July";
                case ("Aug"):
                    return "August";
                case ("Sep"):
                    return "September";
                case ("Nov"):
                    return "November";
                case ("Dec"):
                    return "December";
                default:
                    return ShortHand;
            }
        }
    }

    /* Information used for routing recording sessions to the right person */
    public class PodcastEmail
    {
        public string Podcast { get; set; }
        public string Email { get; set; }
    }
}
