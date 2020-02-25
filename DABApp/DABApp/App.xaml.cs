using System;
using System.Collections.Generic;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;
using System.Diagnostics;
using DABApp.DabSockets;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Device = Xamarin.Forms.Device;

namespace DABApp
{
    public partial class App : Application
    {
        ContentAPI contentAPI = new ContentAPI();
        ContentConfig contentConfig = new ContentConfig();
        public App()
        {
            if (AuthenticationAPI.GetTestMode())
            {
                GlobalResources.TestMode = true;
            }
            InitializeComponent();

            FlowListView.Init();
            List<Versions> versionList = new List<Versions>();
            versionList = contentConfig.versions;
            contentAPI.GetModes();
            if (ContentAPI.CheckContent()) //Check for valid content API
            {
                if (AuthenticationAPI.CheckToken() && versionList == null) //Check to see if the user is logged in.
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
            AppCenter.Start("ios=71f3b832-d6bc-47f3-a1f9-6bbda4669815;" + "android=63fbcb2c-3fcd-4491-b6c3-80f75d2e0d4d;", typeof(Analytics), typeof(Crashes));
        }

        protected override async void OnSleep()
        {
            try
            {

                DabSyncService.Instance.Disconnect(false);
                if (Device.RuntimePlatform == "iOS")
                {
                    AuthenticationAPI.PostActionLogs();
                }
                else await AuthenticationAPI.PostActionLogs();

            }
            catch (Exception ex)
            {

            }
        }

        protected override async void OnResume()
        {
            try
            {


                DabSyncService.Instance.Init();
                DabSyncService.Instance.Connect();

                if (GlobalResources.playerPodcast != null)
                {
                    //Notify bound elements of any changes happened to the player from outside the app (like the lock screen)
                    GlobalResources.playerPodcast.NotifyPlayStateChanged();
                }
                //TODO: Replace this with sync
                //JournalTracker.Current.Open = true;
                if (Device.RuntimePlatform == Device.iOS)
                {
                    AuthenticationAPI.GetMemberData();
                }
                await AuthenticationAPI.GetMemberData();
            }
            catch (Exception ex)
            {

            }
        }


    }
}
