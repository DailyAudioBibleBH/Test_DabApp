using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using DABApp;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

//Attach this class to the ContentPage renderer
[assembly: ExportRenderer(typeof(DabBaseContentPage), typeof(DabBaseContentPageRenderer))]

namespace DABApp.iOS
{
	public class DabBaseContentPageRenderer : PageRenderer
	{
		public DabBaseContentPageRenderer()
		{
			
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			var contentPage = this.Element as ContentPage;
			if (contentPage == null || NavigationController == null)
			{
				return;
			}

			var itemsInfo = contentPage.ToolbarItems;

			var navigationItem = this.NavigationController.TopViewController.NavigationItem;
			var leftNativeButtons = (navigationItem.LeftBarButtonItems ?? new UIBarButtonItem[] { }).ToList();
			var rightNativeButtons = (navigationItem.RightBarButtonItems ?? new UIBarButtonItem[] { }).ToList();
			var rightNativeaButtonsCopy = (navigationItem.RightBarButtonItems ?? new UIBarButtonItem[] { }).ToList();

			rightNativeaButtonsCopy.ForEach(nativeItem =>
			{
			// [Hack] Get Xamarin private field "item"
			var field = nativeItem.GetType().GetField("_item", BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null)
				{
					return;
				}

				var info = field.GetValue(nativeItem) as ToolbarItem;
				if (info != null && info.Priority != 1)
				{
					return;
				}

				rightNativeButtons.Remove(nativeItem);
				leftNativeButtons.Add(nativeItem);
			});

			navigationItem.RightBarButtonItems = rightNativeButtons.ToArray();
			navigationItem.LeftBarButtonItems = leftNativeButtons.ToArray();

		}


	}
}
