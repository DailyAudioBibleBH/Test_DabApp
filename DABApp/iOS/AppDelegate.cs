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

namespace DABApp.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
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

            //Stripe.StripeClient.DefaultPublishableKey = "pk_test_L6czgMBGtoSv82HJgIHGayGO";
            Firebase.Core.App.Configure();

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
    }
}
