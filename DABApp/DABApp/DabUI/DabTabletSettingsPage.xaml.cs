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
			SettingsPage.reset.Tapped += OnReset;
			SettingsPage.appInfo.Tapped += OnAppInfo;
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

		void Remove() {
			if (needRemove) SettingsPage.ToolbarItems.RemoveAt(0);
			needRemove = false;
		}
	}
}
