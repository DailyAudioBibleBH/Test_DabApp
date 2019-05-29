using System;
using System.Collections.Generic;
using System.Linq;
using DLToolkit.Forms.Controls;
using FFImageLoading.Forms.Touch;
using Foundation;
using HockeyApp.iOS;
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

namespace DABApp.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IMessagingDelegate, IUNUserNotificationCenterDelegate
	{

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			SQLitePCL.Batteries.Init();//Setting up the SQLite database to be Serialized prevents a lot of errors when using the database so regularly.
            SQLitePCL.raw.sqlite3_shutdown();
			SQLitePCL.raw.sqlite3_config(Convert.ToInt32(SQLite3.ConfigOption.Serialized));
			SQLitePCL.raw.sqlite3_initialize();

            //Added this to get journaling to work found it here: https://stackoverflow.com/questions/4926676/mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { return true; });

            CachedImageRenderer.Init();
            Rg.Plugins.Popup.Popup.Init();
            

			var manager = BITHockeyManager.SharedHockeyManager;
			manager.Configure("71f3b832d6bc47f3a1f96bbda4669815");
			manager.StartManager();
			manager.Authenticator.AuthenticateInstallation();

			global::Xamarin.Forms.Forms.Init();
			Xamarin.Forms.DependencyService.Register<ShareIntent>();
			DependencyService.Register<SocketService>();
			DependencyService.Register<KeyboardHelper>();
            DependencyService.Register<RecordService>();
            DependencyService.Register<AnalyticsService>();

			SlideOverKit.iOS.SlideOverKit.Init();

            SegmentedControlRenderer.Init();

			app.StatusBarStyle = UIStatusBarStyle.LightContent;

            //Initialize Firebase
            Firebase.Core.App.Configure();

            //Register for remote notifications (Firebase Cloud Messaging)
            // https://firebase.google.com/docs/cloud-messaging/ios/client?authuser=0
            // https://github.com/xamarin/GoogleApisForiOSComponents/blob/master/Firebase.CloudMessaging/component/GettingStarted.md
            //
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = this;
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) => {
                    Console.WriteLine(granted);
                });
            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }
            UIApplication.SharedApplication.RegisterForRemoteNotifications();







            //Assign the messaging delegat to this class
            Messaging.SharedInstance.Delegate = this;
            //WRite the current FCM token
            var token = Messaging.SharedInstance.FcmToken ?? "";
            Console.WriteLine($"FCM token: {token}");


            LoadApplication(new App());

            var m = base.FinishedLaunching(app, options);
            int SystemVersion = Convert.ToInt16(UIDevice.CurrentDevice.SystemVersion.Split('.')[0]);
            if (SystemVersion >= 11)
            {
                GlobalResources.Instance.IsiPhoneX = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom != 0;
            }
            return m;
		}


		public override void OnActivated(UIApplication uiApplication)
		{
			base.OnActivated(uiApplication);
			MessagingCenter.Send<string>("Refresh", "Refresh");
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow)
		{
            if (Device.Idiom == TargetIdiom.Phone)
            {
                return UIInterfaceOrientationMask.Portrait;
            }
            else return UIInterfaceOrientationMask.All;
		}

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

        [Export("messaging:didReceiveRegistrationToken:")]
        public void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
        {
            Console.WriteLine($"Firebase registration token: {fcmToken}");

            // TODO: If necessary send token to application server.
            // Note: This callback is fired at each app startup and whenever a new token is generated.
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            Messaging.SharedInstance.ApnsToken = deviceToken;
        }



    }
}
