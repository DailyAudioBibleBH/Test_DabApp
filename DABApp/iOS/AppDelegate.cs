using System;
using System.Collections.Generic;
using System.Linq;
using DLToolkit.Forms.Controls;
using FFImageLoading.Forms.Touch;
using Foundation;
using PushNotification.Plugin;
using SegmentedControl.FormsPlugin.iOS;
using SQLite;
using UIKit;
using UserNotifications;
using Xamarin.Forms;

namespace DABApp.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			SQLitePCL.Batteries.Init();
			SQLitePCL.raw.sqlite3_shutdown();
			SQLitePCL.raw.sqlite3_config(Convert.ToInt32(SQLite3.ConfigOption.Serialized));
			SQLitePCL.raw.sqlite3_initialize();

			CachedImageRenderer.Init();

			global::Xamarin.Forms.Forms.Init();
			Xamarin.Forms.DependencyService.Register<ShareIntent>();
			DependencyService.Register<SocketService>();
			DependencyService.Register<KeyboardHelper>();

			SlideOverKit.iOS.SlideOverKit.Init();

			SegmentedControlRenderer.Init();

			CrossPushNotification.Initialize<CrossPushNotificationListener>();
			app.StatusBarStyle = UIStatusBarStyle.LightContent;

			Stripe.StripeClient.DefaultPublishableKey = "pk_test_L6czgMBGtoSv82HJgIHGayGO";

			LoadApplication(new App());

			return base.FinishedLaunching(app, options);
		}

		public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
		{

			if (CrossPushNotification.Current is IPushNotificationHandler)
			{
				((IPushNotificationHandler)CrossPushNotification.Current).OnErrorReceived(error);
			}


		}

		public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
		{
			if (CrossPushNotification.Current is IPushNotificationHandler)
			{
				((IPushNotificationHandler)CrossPushNotification.Current).OnRegisteredSuccess(deviceToken);
			}

		}

		public override void DidRegisterUserNotificationSettings(UIApplication application, UIUserNotificationSettings notificationSettings)
		{
			application.RegisterForRemoteNotifications();
		}

		// Uncomment if using remote background notifications. To support this background mode, enable the Remote notifications option from the Background modes section of iOS project properties. (You can also enable this support by including the UIBackgroundModes key with the remote-notification value in your app’s Info.plist file.)
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
			if (CrossPushNotification.Current is IPushNotificationHandler) 
			{
				((IPushNotificationHandler)CrossPushNotification.Current).OnMessageReceived(userInfo);
			}
        }

		public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
		{

			if (CrossPushNotification.Current is IPushNotificationHandler)
			{
				((IPushNotificationHandler)CrossPushNotification.Current).OnMessageReceived(userInfo);
			}
		}

		public override void OnActivated(UIApplication uiApplication)
		{
			base.OnActivated(uiApplication);
			MessagingCenter.Send<string>("Refresh", "Refresh");
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow)
		{
			return UIInterfaceOrientationMask.Portrait;
		}
	}
}
