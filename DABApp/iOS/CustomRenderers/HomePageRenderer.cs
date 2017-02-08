using System;
using CoreGraphics;
using SlideOverKit.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(DABApp.HomePage), typeof(DABApp.iOS.HomePageRenderer))]
namespace DABApp.iOS
{
	public class HomePageRenderer: PageRenderer, ISlideOverKitPageRendereriOS
	{
		public Action<bool> ViewDidAppearEvent { get; set; }

		public Action<VisualElementChangedEventArgs> OnElementChangedEvent { get; set; }

		public Action ViewDidLayoutSubviewsEvent { get; set; }

		public Action<bool> ViewDidDisappearEvent { get; set; }

		public Action<CGSize, IUIViewControllerTransitionCoordinator> ViewWillTransitionToSizeEvent { get; set; }

		public HomePageRenderer()
		{
			new SlideOverKitiOSHandler().Init(this);
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			UIApplication.SharedApplication.BeginReceivingRemoteControlEvents();

			this.BecomeFirstResponder();
		}

		public override void RemoteControlReceived(UIEvent theEvent) 
		{
			if (AudioService.Instance == null) {
				return;
			}
			base.RemoteControlReceived(theEvent);
			AudioService.Instance.RemoteControlReceived(theEvent);
		}
	}
}
