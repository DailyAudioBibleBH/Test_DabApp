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
			//ChangeAvatar();
		}

		void OnSignUp(object o, EventArgs e) {
			SignUp.IsEnabled = false;
			//await DisplayAlert("OH NO!", "Something went wrong, Sorry.", "OK");
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
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
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				Navigation.PushAsync(new DabTabletSettingsPage());
			}
			else
			{
				Navigation.PushAsync(new DabSettingsPage());
			}
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

		async void OnItemTapped(object o, ItemTappedEventArgs e) {
			Nav item = (Nav)e.Item;
			if (item.title == "Channels")
			{
				await Navigation.PopToRootAsync();
			}
			else {
				View view = ContentConfig.Instance.views.Single(x => x.id == item.view);
				if (item.title == "Prayer Wall")
				{
					//var fo = await ContentAPI.GetForum(view);
					if (Device.Idiom == TargetIdiom.Tablet)
					{
						await Navigation.PushAsync(new DabForumTabletTopicPage(view));
					}
					else
					{
						await Navigation.PushAsync(new DabForumPhoneTopicList(view));
					}
					RemovePages();
				}
				else
				{
					if (item.title == "About" && Device.Idiom == TargetIdiom.Tablet)
					{
						await Navigation.PushAsync(new DabParentChildGrid(view));
					}
					else
					{
						await Navigation.PushAsync(new DabContentView(view));
					}
					RemovePages();
				}
			}
			pageList.SelectedItem = null;
		}

		//public void ChangeAvatar() { 
		//	if (GuestStatus.Current.IsGuestLogin)
		//		{
		//			UserName.IsVisible = false;
		//			Avatar.IsVisible = false;
		//			SignUp.IsVisible = true;
		//		}
		//		else
		//		{
		//			UserName.Text = GlobalResources.GetUserName();
		//			if (string.IsNullOrEmpty(GlobalResources.UserAvatar))
		//			{
		//				AvatarSource.Uri = new Uri("http://placehold.it/10x10");
		//			}
		//			else
		//			{
		//				AvatarSource.Uri = new Uri(GlobalResources.UserAvatar);
		//			}
		//		}
		//}
	}
}
