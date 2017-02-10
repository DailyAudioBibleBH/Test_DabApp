using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using SegmentedControl.FormsPlugin.iOS;
using UIKit;

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

			var settings = UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Alert | UIUserNotificationType.Badge, null);
			UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);

			LoadApplication(new App());

			return base.FinishedLaunching(app, options);
		}
	}
}
