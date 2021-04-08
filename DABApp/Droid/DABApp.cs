using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using BranchXamarinSDK;
using DABApp.DabNotifications;
using DABApp.Droid.DependencyServices;
using Plugin.FirebasePushNotification;

namespace DABApp.Droid
{
    [Application(AllowBackup = true, Label = "@string/app_name")]
    [MetaData("io.branch.sdk.auto_link_disable", Value = "false")]
    [MetaData("io.branch.sdk.TestMode", Value = "true")]
    [MetaData("io.branch.sdk.BranchKey", Value = "@string/branch_key")]
    public class DABApp : Application, IBranchSessionInterface
    {
        public static Context AppContext;

        public DABApp(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        public override void OnCreate()
        {
            base.OnCreate();
            BranchAndroid.GetAutoInstance(this.ApplicationContext);
            AppContext = this.ApplicationContext;

            SQLite_Droid.Assets = this.Assets;

            /* FIREBASE CLOUD MESSAGING INIT */

            //Firebase Initialization
            //See this post: https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/docs/GettingStarted.md

            //Set the default notification channel for your app when running Android Oreo
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                //Change for your default notification channel id here
                FirebasePushNotificationManager.DefaultNotificationChannelId = "FirebasePushNotificationChannel";
                //Change for your default notification channel name here
                FirebasePushNotificationManager.DefaultNotificationChannelName = "General";
            }

            //If debug you should reset the token each time.
#if DEBUG
            FirebasePushNotificationManager.Initialize(this, true);
#else
              FirebasePushNotificationManager.Initialize(this,false);
#endif

            //Handle notification when app is closed here
            CrossFirebasePushNotification.Current.OnTokenRefresh += FCM_OnTokenRefresh;
            CrossFirebasePushNotification.Current.OnNotificationReceived += FCM_OnNotificationReceived;
            CrossFirebasePushNotification.Current.OnNotificationOpened += FCM_OnNotificationOpened;
            CrossFirebasePushNotification.Current.OnNotificationAction += FCM_OnNotificationAction;
            CrossFirebasePushNotification.Current.OnNotificationDeleted += FCM_OnNotificationDeleted;

            /* END FIREBASE CLOUD MESSAGING INIT */

        }

        #region IBranchSessionInterface implementation

        public void InitSessionComplete(Dictionary<string, object> data)
        {
        }

        public void CloseSessionComplete()
        {
        }

        public void SessionRequestError(BranchError error)
        {
        }

        #endregion


        /* FIREBASE CLOUD MESSAGING EVENTS */

        void FCM_OnNotificationReceived(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationDataEventArgs e)
        {
            DabPushNotification push = new DabPushNotification(e);
            push.DisplayAlert();
        }

        void FCM_OnNotificationOpened(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationResponseEventArgs e)
        {
            //TODO: Don't think these are displaying popup on Android yet.
            DabPushNotification push = new DabPushNotification(e);
            push.DisplayAlert();
        }

        void FCM_OnTokenRefresh(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationTokenEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"TOKEN : {e.Token}");
        }

        void FCM_OnNotificationAction(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationResponseEventArgs e)
        {
            //TODO: Handle action
            System.Diagnostics.Debug.WriteLine("Action");

            if (!string.IsNullOrEmpty(e.Identifier))
            {
                System.Diagnostics.Debug.WriteLine($"ActionId: {e.Identifier}");
            }
        }

        void FCM_OnNotificationDeleted(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationDataEventArgs e)
        {
            //TODO: Handle push notification deleted (ANDROID ONLY)
            System.Diagnostics.Debug.WriteLine("Deleted");
        }

    }
}
