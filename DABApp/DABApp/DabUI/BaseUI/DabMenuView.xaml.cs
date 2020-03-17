using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabAudio;
using FFImageLoading;
using SlideOverKit;
using Xamarin.Forms;

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
            OnAvatarChanged(this, new EventArgs());
			GuestStatus.Current.AvatarChanged += OnAvatarChanged;
			UserName.Text = GlobalResources.GetUserName();
		}

		void OnSignUp(object o, EventArgs e) {

            //Send info to Firebase analytics that user tapped an action we track
            var info = new Dictionary<string, string>();
            info.Add("title", "signup");
            DependencyService.Get<IAnalyticsService>().LogEvent("action_navigation", info);


            SignUp.IsEnabled = false;
            player.Stop();
			var nav = new NavigationPage(new DabLoginPage());
			nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
			Navigation.PushModalAsync(nav);
			SignUp.IsEnabled = true;
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

			var test = ContentConfig.Instance.views;


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
                    await Navigation.PushAsync(new DabAchievementsPage());
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
                    await Navigation.PushModalAsync(new DabRecordingPage());
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

		async void OnAvatarChanged(object o, EventArgs e)
		{ 
			try
			{
				await ImageService.Instance.LoadUrl(GuestStatus.Current.AvatarUrl).DownloadOnlyAsync();
			}
			catch(Exception ex) {
				Debug.WriteLine($"Error in OnAvatarChanged: {ex.Message}");
			}
		}
	}
}
