using System;
using System.Collections.Generic;
using System.Drawing;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

//Attach this class to the navigationpage renderer
[assembly: ExportRenderer(typeof(NavigationPage), typeof(DabNavigationPageRenderer))]

namespace DABApp.iOS
{
	public class DabNavigationPageRenderer: NavigationRenderer
	{
		public DabNavigationPageRenderer()
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			//Customize the navigation bar 
			this.NavigationBar.TintColor = UIColor.White;
			this.NavigationBar.BarTintColor = UIColor.DarkGray;
			this.NavigationBar.BarStyle = UIBarStyle.BlackTranslucent;

			List<UIBarButtonItem> newRightBtns = new List<UIBarButtonItem>();

			foreach (UIBarButtonItem i in TopViewController.NavigationItem.RightBarButtonItems)
			{
				if (i.Title.Contains("menu"))
				{
					UIButton closeBtn = UIButton.FromType(UIButtonType.System);
					closeBtn.SetImage(UIImage.FromFile("ic_menu_white.png"), UIControlState.Normal);
					closeBtn.Frame = new RectangleF(0, 0, 28, 25);
					closeBtn.AddTarget(i.Target, i.Action, UIControlEvent.TouchUpInside);
					i.CustomView = closeBtn;
					TopViewController.NavigationItem.LeftBarButtonItem = i;
				}
				else {
					newRightBtns.Add(i);
				}
			}
			TopViewController.NavigationItem.RightBarButtonItems = newRightBtns.ToArray();

			//Rearrange the menu drawer
			//List<UIBarButtonItem> newRightBtns = new List<UIBarButtonItem>();
			//foreach (UIBarButtonItem i in TopViewController.NavigationItem.RightBarButtonItems)
			//{
			//	if (i.Title != null)
			//	{
			//		switch (i.Title)
			//		{
			//			case "menu":
			//				UIButton menuButton = UIButton.FromType(UIButtonType.System);
			//				menuButton.SetImage(UIImage.FromFile("ic_menu_white.png"), UIControlState.Normal);
			//				menuButton.Frame = new RectangleF(0, 0, 28, 25);
			//				menuButton.AddTarget(i.Target, i.Action, UIControlEvent.TouchUpInside);
			//				i.CustomView = menuButton;
			//				TopViewController.NavigationItem.LeftBarButtonItem = i;
			//				break;
			//			default:
			//				newRightBtns.Add(i);
			//				break;
			//		}

			//	}
			//	else {
			//		newRightBtns.Add(i);
			//	}
			//}
			//TopViewController.NavigationItem.RightBarButtonItems = newRightBtns.ToArray();
		}
	}
}
