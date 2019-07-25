using System;
using System.Collections.Generic;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;
using System.Diagnostics;
using Xamarin.Forms.PlatformConfiguration;
using DABApp.Interfaces;

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

            if (ContentAPI.CheckContent()) //Check for valid content API
            {
                IAppVersionName service = DependencyService.Get<IAppVersionName>();
                string version = service.GetVersionName();
                if (Device.RuntimePlatform == Device.iOS)
                {
                    //iOS stuff
                    //string version = service.GetVersionName();
                }
                else if (Device.RuntimePlatform == Device.Android)
                {
                    //string version = service.GetVersionName();
                }
                if (AuthenticationAPI.CheckToken() && ContentConfig.Instance.blocktext.mode == null) //Check to see if the user is logged in.
                {
                    MainPage = new NavigationPage(new DabChannelsPage()); //Take to channels page is logged in
                }
                else
                {
                    //Take them to the login page if they aren't logged in or there is a special mode in play.
                    MainPage = new NavigationPage(new DabLoginPage()); //Take to login page if not logged in
                }
            }
            else
            {
                MainPage = new DabNetworkUnavailablePage(); //Take to network unavailable page if not logged in.
            }
            MainPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
        }

        protected override void OnStart()
        {
            DependencyService.Get<IAnalyticsService>().LogEvent("app_startup", "start_date", DateTime.Now.ToShortDateString());
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
