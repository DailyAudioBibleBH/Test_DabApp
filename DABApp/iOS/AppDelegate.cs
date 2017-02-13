using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using PushNotification.Plugin;
using SegmentedControl.FormsPlugin.iOS;
using UIKit;
using UserNotifications;

namespace DABApp.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init();

			SlideOverKit.iOS.SlideOverKit.Init();

			SegmentedControlRenderer.Init();

			CrossPushNotification.Initialize<CrossPushNotificationListener>();
			CrossPushNotification.Current.Register();

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

	}
}
