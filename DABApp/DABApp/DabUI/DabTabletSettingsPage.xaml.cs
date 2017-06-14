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
			SettingsPage.Reset.Clicked += OnReset;
			SettingsPage.Offline.Clicked += OnOffline;
			SettingsPage.AppInfo.Clicked += OnAppInfo;
		}

		void OnMenu(object o, EventArgs e) {
			this.ShowMenu();
		}

		void OnReset(object o, EventArgs e) {
			var ResetPage = new DabResetListenedToStatusPage();
			this.Detail = ResetPage;
			ResetPage.ToolbarItems.Clear();
			//ResetPage.ControlTemplate = playerBarTemplate;
			Remove();
		}

		void OnOffline(object o, EventArgs e) {
			var Offline = new DabOfflineEpisodeManagementPage();
			this.Detail = Offline;
			Offline.ToolbarItems.Clear();
			//Offline.ControlTemplate = playerBarTemplate;
			Remove();
		}

		void OnAppInfo(object o, EventArgs e) {
			var AppInfo = new DabAppInfoPage();
			this.Detail = AppInfo;
			AppInfo.ToolbarItems.Clear();
			//AppInfo.ControlTemplate = playerBarTemplate;
			Remove();
		}

		void Remove() {
			if (needRemove) SettingsPage.ToolbarItems.RemoveAt(0);
			needRemove = false;
		}
	}
}
