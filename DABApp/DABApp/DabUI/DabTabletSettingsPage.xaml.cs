using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTabletSettingsPage : MasterDetailMenuPage
	{
		ControlTemplate playerBarTemplate;
		bool needRemove = true;

		public DabTabletSettingsPage()
		{
			InitializeComponent();
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
		}

		void OnMenu(object o, EventArgs e) {
			this.ShowMenu();
		}

		void OnListening(object o, ItemTappedEventArgs e) {
			//var pre = e.Item as Preset;
			//switch (pre.duration) { 
			//	case "Offline Episodes":
					var Offline = new DabOfflineEpisodeManagementPage();
					this.Detail = Offline;
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
			Detail = AppInfo;
			AppInfo.ToolbarItems.Clear();
			Remove();
		}

		void OnReset(object o, EventArgs e)
		{	
			var Reset = new DabResetListenedToStatusPage();
			Detail = Reset;
			Reset.ToolbarItems.Clear();
			Remove();
		}

		void OnOffline(object o, EventArgs e) {
			var Offline = new DabOfflineEpisodeManagementPage();
			Detail = Offline;
			Offline.ToolbarItems.Clear();
			Remove();
		}

		void OnProfile(object o, EventArgs e) {
			var Profile = new DabProfileManagementPage();
			Detail = Profile;
			Profile.ToolbarItems.Clear();
			Remove();
		}

		void OnAddresses(object o, EventArgs e) {
			var Addresses = new DabAddressManagementPage();
			Detail = new NavigationPage(Addresses);
			Addresses.ToolbarItems.Clear();
			Remove();
		}

		async void OnWallet(object o, EventArgs e) {
			//ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(SettingsPage, "activity");
			//StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(SettingsPage, "activityHolder");
			//activity.IsVisible = true;
			//activityHolder.IsVisible = true;
			var result = await AuthenticationAPI.GetWallet();
			var Wallet = new DabWalletPage(result);
			Detail = new NavigationPage(Wallet);
			Wallet.ToolbarItems.Clear();
			Remove();
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
			Detail = new NavigationPage(Donations);
			Donations.ToolbarItems.Clear();
			Remove();
			//activity.IsVisible = false;
			//activityHolder.IsVisible = false;
		}

		void Remove() {
			if (needRemove) SettingsPage.ToolbarItems.RemoveAt(0);
			needRemove = false;
		}
	}
}
