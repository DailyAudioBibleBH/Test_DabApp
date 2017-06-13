using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTabletSettingsPage : MasterDetailMenuPage
	{
		ControlTemplate playerBarTemplate;

		public DabTabletSettingsPage()
		{
			InitializeComponent();
			playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			NavigationPage.SetTitleIcon(this, "navbarlogo.png");
			this.SlideMenu = new DabMenuView();
			SettingsPage.ControlTemplate = playerBarTemplate;
			AppInfoPage.ControlTemplate = playerBarTemplate;
			SettingsPage.ToolbarItems.Clear();
			AppInfoPage.ToolbarItems.Clear();
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
			ResetPage.ControlTemplate = playerBarTemplate;
		}

		void OnOffline(object o, EventArgs e) {
			var Offline = new DabOfflineEpisodeManagementPage();
			this.Detail = Offline;
			Offline.ToolbarItems.Clear();
			Offline.ControlTemplate = playerBarTemplate;
		}

		void OnAppInfo(object o, EventArgs e) {
			var AppInfo = new DabAppInfoPage();
			this.Detail = AppInfo;
			AppInfo.ToolbarItems.Clear();
			AppInfo.ControlTemplate = playerBarTemplate;
		}
	}
}
