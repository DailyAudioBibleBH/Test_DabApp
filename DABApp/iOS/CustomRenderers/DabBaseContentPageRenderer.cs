using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using CoreGraphics;
using DABApp;
using DABApp.iOS;
using SlideOverKit.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Color = Xamarin.Forms.Color;

//Attach this class to the ContentPage renderer
[assembly: ExportRenderer(typeof(DabBaseContentPage), typeof(DabBaseContentPageRenderer))]

namespace DABApp.iOS
{
	public class DabBaseContentPageRenderer : PageRenderer, ISlideOverKitPageRendereriOS
	{
        public new DabBaseContentPage Element
        {
            get { return (DabBaseContentPage)base.Element; }
        }

		public Action<bool> ViewDidAppearEvent { get; set; }

		public Action<VisualElementChangedEventArgs> OnElementChangedEvent { get; set; }

		public Action ViewDidLayoutSubviewsEvent { get; set; }

		public Action<bool> ViewDidDisappearEvent { get; set; }

		public Action<CGSize, IUIViewControllerTransitionCoordinator> ViewWillTransitionToSizeEvent { get; set; }

		public DabBaseContentPageRenderer()
		{
			new SlideOverKitiOSHandler().Init(this);
		}

		 protected override void OnElementChanged (VisualElementChangedEventArgs e)
        {
            base.OnElementChanged (e);

            if (OnElementChangedEvent != null)
                OnElementChangedEvent (e);
        }

        public override void ViewDidLayoutSubviews ()
        {
            base.ViewDidLayoutSubviews ();
            if (ViewDidLayoutSubviewsEvent != null)
                ViewDidLayoutSubviewsEvent ();

        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            if (ViewDidAppearEvent != null)
                ViewDidAppearEvent (animated);
        }

        public override void ViewDidDisappear (bool animated)
        {
            base.ViewDidDisappear (animated);          
            if (ViewDidDisappearEvent != null)
                ViewDidDisappearEvent (animated);
        }

        public override void ViewWillTransitionToSize (CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize (toSize, coordinator);
            if (ViewWillTransitionToSizeEvent != null)
                ViewWillTransitionToSizeEvent (toSize, coordinator);            
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

			//Adds left buttons beside the back button
			navigationItem.LeftItemsSupplementBackButton = true; 

			//Set the navigation bar buttons
			navigationItem.RightBarButtonItems = rightNativeButtons.ToArray();
			navigationItem.LeftBarButtonItems = leftNativeButtons.ToArray();
            var recordButton = navigationItem.RightBarButtonItems.LastOrDefault();
            if (recordButton != null)
            {
                if (Device.Idiom == TargetIdiom.Phone)
                { 
                    var r = new UICustomButton();
                    r.ImageEdgeInsets = new UIEdgeInsets(6, 30, 6, -12);
                    r.SetImage(recordButton.Image, UIControlState.Normal);
                    r.TranslatesAutoresizingMaskIntoConstraints = false;
                    r.TouchUpInside += async delegate {
                        GlobalResources.GoToRecordingPage();
                    };
                    //r.AddTarget(r.Self, recordButton.Action, UIControlEvent.TouchUpInside);
                    recordButton.CustomView = r;
                }
                else
                {
                    recordButton.ImageInsets = new UIEdgeInsets(0, 7, 14, 7);
                }
                recordButton.TintColor = ((Color)App.Current.Resources["RecordColor"]).ToUIColor();
                recordButton.AccessibilityHint = "Record";
            }

            var menuButton = navigationItem.LeftBarButtonItem;
            if (menuButton != null)
            {
                menuButton.AccessibilityHint = "Menu";
                menuButton.ImageInsets = new UIEdgeInsets(0, -10, 0, 10);
            }

        }


	}
}

namespace DABApp.iOS {
    public class UICustomButton : UIButton {
        public override UIButtonType ButtonType { get { return UIButtonType.Custom; } }
        public override UIEdgeInsets AlignmentRectInsets { get; } = new UIEdgeInsets(0, 20, 0, -20);
    }
}
