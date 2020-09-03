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
using Xamarin.Forms.Internals;
using DABApp.Service;
using DABApp.DabUI;

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
            Xamarin.Forms.Internals.Log.Listeners.Add(new DelegateLogListener((arg1, arg2) => Debug.WriteLine(arg2)));
            InitializeComponent();

            FlowListView.Init();
            if (AuthenticationAPI.GetExperimentMode())
            {
                GlobalResources.ExperimentMode = true;
                GlobalResources.SetDisplay();
            }
            MainPage = new DabServiceConnect();

        }

        protected override void OnStart()
        {
            /* 
             * App is starting up
             */

            DependencyService.Get<IAnalyticsService>().LogEvent("app_startup", "start_date", DateTime.Now.ToShortDateString());
            AppCenter.Start("ios=71f3b832-d6bc-47f3-a1f9-6bbda4669815;" + "android=63fbcb2c-3fcd-4491-b6c3-80f75d2e0d4d;", typeof(Analytics), typeof(Crashes));
        }

        protected override async void OnSleep()
        {
            try
            {
                //put the service to sleep
                var ql = await DabService.TerminateConnection();
            }
            catch (Exception ex)
            {

            }
        }

        protected override async void OnResume()
        {
            try
            {
                //reconnect to service
                var ql = await DabService.InitializeConnection();

                if (ql.Success)
                {
                    //perform post-connection operations with service
                    await DabServiceRoutines.RunConnectionEstablishedRoutines();
                }

                //TODO: Old code is below - may need revamped

                if (GlobalResources.playerPodcast != null)
                {
                    //Notify bound elements of any changes happened to the player from outside the app (like the lock screen)
                    GlobalResources.playerPodcast.NotifyPlayStateChanged();
                }

                //Notify UI that app is active again
                MessagingCenter.Send<string>("DabApp", "OnResume");

            }
            catch (Exception ex)
            {

            }
        }
    }
}
