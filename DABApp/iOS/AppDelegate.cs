using System;
using System.Collections.Generic;
using System.Linq;
using DLToolkit.Forms.Controls;
using Foundation;
using SegmentedControl.FormsPlugin.iOS;
using SQLite;
using UIKit;
using UserNotifications;
using Xamarin.Forms;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Plugin.AudioRecorder;
using Firebase.CloudMessaging;
using AVFoundation;
using MediaPlayer;
using Plugin.FirebasePushNotification;
using DABApp.DabNotifications;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using BranchXamarinSDK;
using System.Collections;
using Version.Plugin;
using System.Diagnostics;

namespace DABApp.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IMessagingDelegate, IUNUserNotificationCenterDelegate, IBranchBUOSessionInterface
    {

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            //SQL Lite Init
            SQLitePCL.Batteries.Init();//Setting up the SQLite database to be Serialized prevents a lot of errors when using the database so regularly.
            SQLitePCL.raw.sqlite3_shutdown();
            SQLitePCL.raw.sqlite3_config(Convert.ToInt32(SQLite3.ConfigOption.Serialized));
            SQLitePCL.raw.sqlite3_initialize();

            //Added this to get journaling to work found it here: https://stackoverflow.com/questions/4926676/mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { return true; });

            //Popup INit
            Rg.Plugins.Popup.Popup.Init();

            //Ios Nav bar decoration
            UINavigationBar.Appearance.TintColor = UIColor.FromRGB(203, 203, 203);
            UITextAttributes att = new UITextAttributes();
            att.TextColor = UIColor.FromRGB(203, 203, 203);
            att.Font = UIFont.FromName("FetteEngD", 25);
            UINavigationBar.Appearance.SetTitleTextAttributes(att);

            global::Xamarin.Forms.Forms.Init();

            /* Branch.IO DeepLinking */
            // Debug mode - set to 'false' before releasing to production
            BranchIOS.Debug = true;

            DABAppBUO appBUO = new DABAppBUO();
            BranchIOS.Init("key_live_liAnF8k7gZUEZv76Rt9a4bffAzlC5zVW", options, appBUO);
            LoadApplication(appBUO);

            //TODO: Replace for journal?
            //DependencyService.Register<SocketService>();
            DependencyService.Register<KeyboardHelper>();
            DependencyService.Register<RecordService>();
            DependencyService.Register<AnalyticsService>();

            //Slideover Kit Init
            SlideOverKit.iOS.SlideOverKit.Init();

            SegmentedControlRenderer.Init();

            app.StatusBarStyle = UIStatusBarStyle.LightContent;
            //if version 10 or higher
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = this;
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
                {
                    Console.WriteLine(granted);
                });
            }
            //iOS version 9 or before
            else
            {
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }

            LoadApplication(new App());

            // AppCenter Crash & Analytic Reporting
            AppCenter.Start("71f3b832-d6bc-47f3-a1f9-6bbda4669815", typeof(Analytics), typeof(Crashes));


            /* END AUDIO PLAYER DEFAULTS

            /* FIREBASE CLOUD MESSAGING */
            // See https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/docs/GettingStarted.md
            FirebasePushNotificationManager.Initialize(options, true);
            CrossFirebasePushNotification.Current.OnTokenRefresh += FCM_OnTokenRefresh;
            CrossFirebasePushNotification.Current.OnNotificationReceived += FCM_OnNotificationReceived;
            CrossFirebasePushNotification.Current.OnNotificationOpened += FCM_OnNotificationOpened;
            CrossFirebasePushNotification.Current.OnNotificationAction += FCM_OnNotificationAction;
            CrossFirebasePushNotification.Current.OnNotificationDeleted += FCM_OnNotificationDeleted;             //Push message deleted event usage sample: (Android Only)


            /* END FIREBASE CLOUD MESSAGING */

            var m = base.FinishedLaunching(app, options);
            int SystemVersion = Convert.ToInt16(UIDevice.CurrentDevice.SystemVersion.Split('.')[0]);
            if (SystemVersion >= 11)
            {
                GlobalResources.Instance.IsiPhoneX = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom != 0;
            }

            //first launch of app, need to ask permissions
            if (dbSettings.GetSetting("AppVersion", "") == "")
            {
                dbSettings.StoreSetting("AppVersion", CrossVersion.Current.Version);
                PromptUserForPermissions();
            }

            return m;
        }

        private async void PromptUserForPermissions()
        {
            //if version 14 or higher
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
                await App.Current.MainPage.DisplayAlert("App Tracking Settings", "The next prompt you will receive will ask permission to share analytical information with DAB. We ask that you say yes to help us get more accurate error reporting and finding solutions. We value our users & would never sell your information or deliver targeted Ads towards you.", "Okay");

                //Request Permission to follow AppTrackingTransparency guidelines
                AppTrackingTransparency.ATTrackingManager.RequestTrackingAuthorization((result) =>
                {
                    switch (result)
                    {
                        case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.NotDetermined:
                            Firebase.Analytics.Analytics.SetUserProperty("false", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                            Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(false);
                            break;
                        case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Restricted:
                            Firebase.Analytics.Analytics.SetUserProperty("false", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                            Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(false);
                            break;
                        case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Denied:
                            Firebase.Analytics.Analytics.SetUserProperty("false", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                            Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(false);
                            break;
                        case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Authorized:
                            Firebase.Analytics.Analytics.SetUserProperty("true", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                            Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(true);
                            dbSettings.StoreSetting("AppVersion", CrossVersion.Current.Version);
                            break;
                        default:
                            break;
                    }
                });
            }
        }

        /* Branch.IO DeepLinking */

        public override UIWindow Window
        {
            get;
            set;
        }

        // Called when the app is opened via URI scheme
        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            return BranchIOS.getInstance().OpenUrl(url);
        }

        // Called when the app is opened from a Universal Link
        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            return BranchIOS.getInstance().ContinueUserActivity(userActivity);
        }

        // Called when the app receives a push notification
        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            BranchIOS.getInstance().HandlePushNotification(userInfo);
        }

        // Called when the Branch initialization is completed
        // Put deep-linking logic in this method
        public void InitSessionComplete(BranchUniversalObject buo, BranchLinkProperties blp)
        {
            NSObject[] keys = {
                NSObject.FromObject("+is_first_session")
            };

            NSObject[] values = { NSObject.FromObject(0) };
            //if (buo.metadata.ContainsKey("+is_first_session"))
            //{
            //    values[0] = NSObject.FromObject(buo.metadata["+is_first_session"]);
            //}

            NSDictionary nsData = NSDictionary.FromObjectsAndKeys(values, keys);
        }

        // Called when there is an error initializing Branch
        public void SessionRequestError(BranchError error)
        {
            Console.WriteLine("Branch error: " + error.ErrorCode);
            Console.WriteLine(error.ErrorMessage);
        }

        /* End Branch.IO DeepLinking */


        /* GENERAL UI EVENTS */

        public override void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);
            MessagingCenter.Send<string>("Refresh", "Refresh");
        }

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow)
        {
            //Detect Orientation of device
            if (Xamarin.Forms.Device.Idiom == TargetIdiom.Phone)
            {
                return UIInterfaceOrientationMask.Portrait;
            }
            else return UIInterfaceOrientationMask.All;
        }

        /* END GENERAL UI EVENTS */



        /* JOURNALLING EVENTS */

        //More of what was needed to get journaling to work on Android once again found it here: https://stackoverflow.com/questions/4926676/mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed
        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //Return true if the server certificate is ok
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            bool acceptCertificate = true;
            string msg = "The server could not be validated for the following reason(s):\r\n";

            //The server did not present a certificate
            if ((sslPolicyErrors &
                 SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                msg = msg + "\r\n    -The server did not present a certificate.\r\n";
                acceptCertificate = false;
            }
            else
            {
                //The certificate does not match the server name
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    msg = msg + "\r\n    -The certificate name does not match the authenticated name.\r\n";
                    acceptCertificate = false;
                }

                //There is some other problem with the certificate
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    foreach (X509ChainStatus item in chain.ChainStatus)
                    {
                        if (item.Status != X509ChainStatusFlags.RevocationStatusUnknown &&
                            item.Status != X509ChainStatusFlags.OfflineRevocation)
                            break;

                        if (item.Status != X509ChainStatusFlags.NoError)
                        {
                            msg = msg + "\r\n    -" + item.StatusInformation;
                            acceptCertificate = false;
                        }
                    }
                }
            }

            //If Validation failed, present message box
            if (acceptCertificate == false)
            {
                msg = msg + "\r\nDo you wish to override the security check?";
                //          if (MessageBox.Show(msg, "Security Alert: Server could not be validated",
                //                       MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                acceptCertificate = true;
            }

            return acceptCertificate;
        }

        /* END JOURNALING EVENTS */


        /* FIREBASE CLOUD MESSAGING EVENTS */

        void FCM_OnTokenRefresh(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationTokenEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"TOKEN : {e.Token}");
        }

        void FCM_OnNotificationReceived(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationDataEventArgs e)
        {
            DabPushNotification push = new DabPushNotification(e);
            push.DisplayAlert();
        }

        void FCM_OnNotificationOpened(object source, Plugin.FirebasePushNotification.Abstractions.FirebasePushNotificationResponseEventArgs e)
        {
            DabPushNotification push = new DabPushNotification(e);
            push.DisplayAlert();
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

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            FirebasePushNotificationManager.DidRegisterRemoteNotifications(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            FirebasePushNotificationManager.RemoteNotificationRegistrationFailed(error);

        }
        // To receive notifications in foregroung on iOS 9 and below.
        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            // If you are receiving a notification message while your app is in the background,
            // this callback will not be fired 'till the user taps on the notification launching the application.

            // If you disable method swizling, you'll need to call this method. 
            // This lets FCM track message delivery and analytics, which is performed
            // automatically with method swizzling enabled.
            FirebasePushNotificationManager.DidReceiveMessage(userInfo);
            // Do your magic to handle the notification data
            System.Console.WriteLine(userInfo);

            completionHandler(UIBackgroundFetchResult.NewData);
        }

        /* END FIREBASE CLOUD MESSAGING EVENTS */



    }
}
