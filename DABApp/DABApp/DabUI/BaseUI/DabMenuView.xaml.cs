using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabMenuView : SlideMenuView
	{
		List<string> pages;

		public DabMenuView()
		{
			pages = new List<string>();
			pages.Add("About");
			pages.Add("Settings");

			InitializeComponent();

			// You must set IsFullScreen in this case, 
			// otherwise you need to set HeightRequest, 
			// just like the QuickInnerMenu sample
			this.IsFullScreen = true;

			// You must set WidthRequest in this case
			this.WidthRequest = 250;
			this.MenuOrientations = MenuOrientation.LeftToRight;

			// You must set BackgroundColor, 
			// and you cannot put another layout with background color cover the whole View
			// otherwise, it cannot be dragged on Android
			//this.BackgroundColor = Color.White; //This is actually overridden by the menu view XAML

			// This is shadow view color, you can set a transparent color
			this.BackgroundViewColor = ((Color)App.Current.Resources["PageBackgroundColor"]).MultiplyAlpha(.75);
			UserName.Text = GlobalResources.GetUserName();
			if (string.IsNullOrEmpty(GlobalResources.UserAvatar))
			{
				AvatarSource.Uri = new Uri("http://placehold.it/10x10");
			}
			else
			{
				AvatarSource.Uri = new Uri(GlobalResources.UserAvatar);
			}
		}

		//Menu Actions

		//void OnChannels(object o, EventArgs e)
		//{
		//	Navigation.PopToRootAsync();
		//}

		void OnSettings(object o, EventArgs e)
		{
			Navigation.PushAsync(new DabSettingsPage());
			RemovePages();
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

		void OnItemTapped(object o, ItemTappedEventArgs e) {
			Nav item = (Nav)e.Item;
			if (item.title == "Channels")
			{
				Navigation.PopToRootAsync();
			}
			else {
				View view = ContentConfig.Instance.views.Single(x => x.id == item.view);
				if (item.title == "Prayer Wall")
				{
					Navigation.PushAsync(new DabPrayerWallPage(view));
					RemovePages();
				}
				else
				{
					Navigation.PushAsync(new DabContentView(view));
					RemovePages();
				}
			}
		}
	}
}
