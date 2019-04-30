using System;
using System.Collections.Generic;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;

namespace DABApp
{
	public partial class App : Application
	{
		public App()
		{
            if (AuthenticationAPI.GetTestMode())
            {
               GlobalResources.TestMode = true;
            }
			InitializeComponent();

			FlowListView.Init();

			if (ContentAPI.CheckContent())
			{
				if (AuthenticationAPI.CheckToken()) {
					MainPage = new NavigationPage(new DabChannelsPage());
				}
				else
				{
					MainPage = new NavigationPage(new DabLoginPage());
				}
			}
			else {
				MainPage = new DabNetworkUnavailablePage();
			}
			MainPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
		}

		protected override void OnStart()
		{
            DependencyService.Get<IAnalyticsService>().LogEvent("app_startup","start_date", DateTime.Now.ToShortDateString());
        }

		protected override async void OnSleep()
		{
            if (Device.RuntimePlatform == "iOS")
            {
                AuthenticationAPI.PostActionLogs();
            }
            else await AuthenticationAPI.PostActionLogs();
			JournalTracker.Current.Open = false;
		}

		protected override async void OnResume()
		{
			JournalTracker.Current.Open = true;
            if (Device.RuntimePlatform == Device.iOS)
            {
                AuthenticationAPI.GetMemberData();
            }
            await AuthenticationAPI.GetMemberData();
		}

		
	}
}
