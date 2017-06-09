using System;
using DABApp.iOS;
using Foundation;
using UIKit;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(ShareIntent))]
namespace DABApp.iOS
{
	public class ShareIntent : IShareable
	{
		public void OpenShareIntent(string Channelcode, string episodeId)
		{
			var activityController = new UIActivityViewController(new NSObject[] { UIActivity.FromObject($"https://player.dailyaudiobible.com/{Channelcode}/{episodeId}") }, null);
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				var popover = activityController.PopoverPresentationController;
				if (popover != null) {
					var self = UIApplication.SharedApplication.KeyWindow.RootViewController;
					popover.SourceView = self.View;
					var frame = UIScreen.MainScreen.Bounds;
					frame.Height /= 4;
					popover.SourceRect = frame;
					popover.PermittedArrowDirections = UIPopoverArrowDirection.Unknown;
				}
			}
			UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(activityController, true, null);
		}
	}
}
