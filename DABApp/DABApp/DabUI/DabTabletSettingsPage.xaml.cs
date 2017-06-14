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
			NavigationPage.SetTitleIcon(this, "navbarlogo.png");
			this.SlideMenu = new DabMenuView();
			SettingsPage.ControlTemplate = playerBarTemplate;
			//AppInfoPage.ControlTemplate = playerBarTemplate;
			AppInfoPage.ToolbarItems.Clear();
			//SettingsPage.ToolbarItems.Clear();
			SettingsPage.listening.ItemTapped += OnListening;
			SettingsPage.other.ItemTapped += OnOther;
		}

		void OnMenu(object o, EventArgs e) {
			this.ShowMenu();
		}

		void OnListening(object o, ItemTappedEventArgs e) {
			var pre = e.Item as Preset;
			switch (pre.duration) { 
				case "Offline Episodes":
					var Offline = new DabOfflineEpisodeManagementPage();
					this.Detail = Offline;
					Offline.ToolbarItems.Clear();
					Remove();
					break;
				case "Reset listened to status":
					var Reset = new DabResetListenedToStatusPage();
					this.Detail = Reset;
					Reset.ToolbarItems.Clear();
					Remove();
					break;
			}
		}

		void OnOther(object o, ItemTappedEventArgs e) {
			var pre = e.Item as Preset;
			switch (pre.duration) {
				case "App info":
					var AppInfo = new DabAppInfoPage();
					this.Detail = AppInfo;
					AppInfo.ToolbarItems.Clear();
					Remove();
					break;
			}
		}

		void OnReset(object o, EventArgs e) {
			var ResetPage = new DabResetListenedToStatusPage();
			this.Detail = ResetPage;
			ResetPage.ToolbarItems.Clear();
			//ResetPage.ControlTemplate = playerBarTemplate;
			Remove();
		}

		void Remove() {
			if (needRemove) SettingsPage.ToolbarItems.RemoveAt(0);
			needRemove = false;
		}
	}
}
