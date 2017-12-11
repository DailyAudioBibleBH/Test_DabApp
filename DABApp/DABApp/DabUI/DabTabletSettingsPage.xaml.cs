using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTabletSettingsPage : MasterDetailMenuPage
	{
		ControlTemplate playerBarTemplate;
		bool needRemove = Device.RuntimePlatform == "iOS" ? true : false;

		public DabTabletSettingsPage()
		{
			InitializeComponent();
            //NavigationPage.SetHasBackButton(this, false);
			playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			Title = "DAILY AUDIO BIBLE";
			this.SlideMenu = new DabMenuView();
			SettingsPage.ControlTemplate = playerBarTemplate;
			//AppInfoPage.ControlTemplate = playerBarTemplate;
			AppInfoPage.ToolbarItems.Clear();
			//SettingsPage.ToolbarItems.Clear();
			SettingsPage.offline.Tapped += OnOffline;
			//SettingsPage.reset.Tapped += OnReset;
			SettingsPage.appInfo.Tapped += OnAppInfo;
			SettingsPage.profile.Tapped += OnProfile;
			SettingsPage.addresses.Tapped += OnAddresses;
			SettingsPage.wallet.Tapped += OnWallet;
			SettingsPage.donations.Tapped += OnDonations;
            SettingsPage.Disappearing += OnDisappearing;
            SettingsPage.Appearing += OnAppearing;
            //SettingsPage.Appearing += OnMenu;
            if (Device.RuntimePlatform == "Android")
            {
                SettingsPage.ToolbarItems.Clear();
                AppInfoPage.Unsubscribe();
                //SettingsPage.Unsubscribe();
                MessagingCenter.Subscribe<string>("Menu", "Menu", (sender) =>
                {
                    SettingsPage.ShowMenu();  
                });
            }
        }

		void OnMenu(object o, EventArgs e) {
			this.ShowMenu();
		}

		void OnListening(object o, ItemTappedEventArgs e) {
			//var pre = e.Item as Preset;
			//switch (pre.duration) { 
			//	case "Offline Episodes":
					var Offline = new DabOfflineEpisodeManagementPage();
					this.Detail = new NavigationPage(Offline);
					Offline.ToolbarItems.Clear();
					Remove();
			//		break;
			//	case "Reset listened to status":
			//		var Reset = new DabResetListenedToStatusPage();
			//		this.Detail = Reset;
			//		Reset.ToolbarItems.Clear();
			//		Remove();
			//		break;
			//}
		}

		void OnAppInfo(object o, EventArgs e) {
			var AppInfo = new DabAppInfoPage();
            AppInfo.Unsubscribe();
			Detail = new NavigationPage(AppInfo) { BarTextColor = (Color)App.Current.Resources["TextColor"] };
			AppInfo.ToolbarItems.Clear();
			Remove();
		}

		void OnReset(object o, EventArgs e)
		{	
			var Reset = new DabResetListenedToStatusPage();
            Reset.Unsubscribe();
			Detail = new NavigationPage(Reset);
			Reset.ToolbarItems.Clear();
			Remove();
		}

		void OnOffline(object o, EventArgs e) {
			var Offline = new DabOfflineEpisodeManagementPage();
            Offline.Unsubscribe();
			Detail = new NavigationPage(Offline) { BarTextColor = (Color)App.Current.Resources["TextColor"] };
			Offline.ToolbarItems.Clear();
			Remove();
		}

		void OnProfile(object o, EventArgs e) {
			var Profile = new DabProfileManagementPage();
            Profile.Unsubscribe();
			Detail = new NavigationPage(Profile) { BarTextColor = (Color)App.Current.Resources["TextColor"] };
			Profile.ToolbarItems.Clear();
			Remove();
		}

		void OnAddresses(object o, EventArgs e) {
			var Addresses = new DabAddressManagementPage();
            Addresses.Unsubscribe();
			Detail = new NavigationPage(Addresses) { BarTextColor = (Color)App.Current.Resources["TextColor"] };
			Addresses.ToolbarItems.Clear();
			Remove();
		}

		async void OnWallet(object o, EventArgs e) {
			//ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(SettingsPage, "activity");
			//StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(SettingsPage, "activityHolder");
			//activity.IsVisible = true;
			//activityHolder.IsVisible = true;
			var result = await AuthenticationAPI.GetWallet();
			if (result != null)
			{
				var Wallet = new DabWalletPage(result);
                Wallet.Unsubscribe();
				Detail = new NavigationPage(Wallet) { BarTextColor = (Color)App.Current.Resources["TextColor"] };
				Wallet.ToolbarItems.Clear();
				Remove();
			}
			else await DisplayAlert("Unable to retrieve Wallet information", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
			//activity.IsVisible = false;
			//activityHolder.IsVisible = false;
		}

		async void OnDonations(object o, EventArgs e) {
			//ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(SettingsPage, "activity");
			//StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(SettingsPage, "activityHolder");
			//activity.IsVisible = true;
			//activityHolder.IsVisible = true;
			var don = await AuthenticationAPI.GetDonations();
			var Donations = new DabManageDonationsPage(don);
            Donations.Unsubscribe();
			Detail = new NavigationPage(Donations) { BarTextColor = (Color)App.Current.Resources["TextColor"]};
			Donations.ToolbarItems.Clear();
			Remove();
			//activity.IsVisible = false;
			//activityHolder.IsVisible = false;
		}

        void OnDisappearing(object o, EventArgs e)
        {
            MessagingCenter.Unsubscribe<string>("Menu", "Menu");
        }

        void OnAppearing(object o, EventArgs e)
        {
            MessagingCenter.Subscribe<string>("Menu", "Menu", (sender) => { ((DabSettingsPage)Master).ShowMenu(); });
        }

        void Remove()
        {
            if (needRemove) SettingsPage.ToolbarItems.RemoveAt(0);
            needRemove = false;
        }
	}
}
