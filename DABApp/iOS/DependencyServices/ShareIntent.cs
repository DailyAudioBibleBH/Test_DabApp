using System;
using DABApp.iOS;
using Foundation;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(ShareIntent))]
namespace DABApp.iOS
{
	public class ShareIntent : IShareable
	{
		public void OpenShareIntent(string Channelcode, string episodeId)
		{
			var activityController = new UIActivityViewController(new NSObject[] { UIActivity.FromObject($"https://player.dailyaudiobible.com/{Channelcode}/{episodeId}") }, null);
			UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(activityController, true, null);
		}
	}
}
