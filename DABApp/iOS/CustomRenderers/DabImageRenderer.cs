using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using CoreGraphics;
using DABApp;

[assembly: ExportRenderer(typeof(PlayerImage), typeof(DabImageRenderer))]
namespace DABApp.iOS
{
	public class DabImageRenderer: ImageRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			base.OnElementChanged(e);
			if (Control != null)
			{
                Control.IsAccessibilityElement = true;
			}
		}
	}
}
