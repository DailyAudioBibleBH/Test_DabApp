using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabAudio;
using SlideOverKit;
using Version.Plugin;
using Xamarin.Forms;
using DABApp.Service;

namespace DABApp
{
	public partial class DabMenuView : SlideMenuView
	{
		List<string> pages;
        private DabPlayer player = GlobalResources.playerPodcast;

		public DabMenuView()
		{
			pages = new List<string>();
			pages.Add("About");
			pages.Add("Settings");
			pages.Add("Achievements");
			pages.Add("Send Audio Recording");
			InitializeComponent();

			// You must set IsFullScreen in this case, 
			// otherwise you need to set HeightRequest, 
			// just like the QuickInnerMenu sample
			IsFullScreen = true;
			// You must set WidthRequest in this case
			WidthRequest = 250;
		    MenuOrientations = MenuOrientation.LeftToRight;

            // You must set BackgroundColor,
            // and you cannot put another layout with background color cover the whole View
            // otherwise, it cannot be dragged on Android
            //this.BackgroundColor = Color.White; //This is actually overridden by the menu view XAML

            // This is shadow view color, you can set a transparent color
            BackgroundViewColor = ((Color)App.Current.Resources["PageBackgroundColor"]).MultiplyAlpha(.75);
            //OnAvatarChanged(this, new EventArgs());
			//GuestStatus.Current.AvatarChanged += OnAvatarChanged;
			GuestStatus.Current.UserName = GlobalResources.GetUserName();

            lblVersion.Text = $"v {CrossVersion.Current.Version}";

			// hide nav items where view is marked as logged in only
			if (GuestStatus.Current.IsGuestLogin == true)
            {
				//only show "both" visibility items (hide achievements in guest mode)
				pageList.ItemsSource = ContentConfig.Instance.nav.Where(n => ContentConfig.Instance.views.Where(v=> v.visible == ViewVisibility.both).Select(v => v.id).Contains(n.view));
            } else
            {
				pageList.ItemsSource = ContentConfig.Instance.nav; //show all
			}


			DabServiceEvents.UserProfileChangedEvent += DabServiceEvents_UserProfileChangedEvent;

			//access utility via triple tap
			var tapper = new TapGestureRecognizer();
			tapper.NumberOfTapsRequired = 3;
			tapper.Tapped += (sender, e) =>
			{
				Navigation.PushAsync(new DabUtilityPage());
			};
			this.lblVersion.GestureRecognizers.Add(tapper);


		}

		private void DabServiceEvents_UserProfileChangedEvent(DabSockets.GraphQlUser user)
        {
			this.UserName.Text = $"{user.firstName} {user.lastName}";
        }

        void OnSignUp(object o, EventArgs e) {

            //Send info to Firebase analytics that user tapped an action we track
            var info = new Dictionary<string, string>();
            info.Add("title", "signup");
            DependencyService.Get<IAnalyticsService>().LogEvent("action_navigation", info);

			GlobalResources.LogoffAndResetApp();
		}

       

		//Menu Actions

		//void OnChannels(object o, EventArgs e)
		//{
		//	Navigation.PopToRootAsync();
		//}

		void OnSettings(object o, EventArgs e)
		{
            //Send info to Firebase analytics that user tapped an action we track
            var info = new Dictionary<string, string>();
            info.Add("title", "settings");
            DependencyService.Get<IAnalyticsService>().LogEvent("action_navigation", info);

            Settings.IsEnabled = false;
			if (GlobalResources.ShouldUseSplitScreen)
			{
               Navigation.PushAsync(new DabTabletSettingsPage());
			}
			else
			{
				Navigation.PushAsync(new DabSettingsPage());
			}
			RemovePages();
            Settings.IsEnabled = true;
		}

		void RemovePages()
		{
			var existingPages = Navigation.NavigationStack.ToList();
			foreach (var page in existingPages)
			{
				if (page != existingPages[0] && page != existingPages.Last())
				{
					Navigation.RemovePage(page);
				}
			}
		}

		async void OnItemTapped(object o, ItemTappedEventArgs e) {
			if (Device.RuntimePlatform == "Android") 
			{ 
				MessagingCenter.Send("Show", "Show"); 
			}
			Nav item = (Nav)e.Item;
            View view = ContentConfig.Instance.views.Single(x => x.id == item.view);

            //Send info to Firebase analytics that user tapped a menu item
            var info = new Dictionary<string, string>();
            info.Add("title", item.title);
            DependencyService.Get<IAnalyticsService>().LogEvent("action_navigation", info);

            switch (item.title)
            {
                case "Channels":
                    await Navigation.PopToRootAsync();
                    if (Device.RuntimePlatform == "iOS") { ((DabBaseContentPage)Parent).HideMenu(); }
                    break;
                case "Achievements":
                    await Navigation.PushAsync(new DabAchievementsPage(view));
                    if (Device.RuntimePlatform == "iOS") { ((DabBaseContentPage)Parent).HideMenu(); }
                    break;
                case "Prayer Wall":
                    if (Device.Idiom == TargetIdiom.Tablet)
                    {
                        await Navigation.PushAsync(new DabForumTabletTopicPage(view));
                    }
                    else
                    {
                        await Navigation.PushAsync(new DabForumPhoneTopicList(view));
                    }
                    RemovePages();
                    break;
                case "Send Audio Recording":
					GlobalResources.GoToRecordingPage();
                    break;
                default:
                    if (item.title == "About" && Device.Idiom == TargetIdiom.Tablet)
                    {
                        await Navigation.PushAsync(new DabParentChildGrid(view));
                    }
                    else
                    {
                        await Navigation.PushAsync(new DabContentView(view));
                    }
                    RemovePages();
                    break;
            }
			
			pageList.SelectedItem = null;
		}

		//async void OnAvatarChanged(object o, EventArgs e)
		//{ 
		//	try
		//	{
		//		await ImageService.Instance.LoadUrl(GuestStatus.Current.AvatarUrl).DownloadOnlyAsync();
		//	}
		//	catch(Exception ex) {
		//		Debug.WriteLine($"Error in OnAvatarChanged: {ex.Message}");
		//	}
		//}
	}
}
