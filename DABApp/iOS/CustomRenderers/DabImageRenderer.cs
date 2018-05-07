using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using CoreGraphics;

[assembly: ExportRenderer(typeof(Image), typeof(DabImageRenderer))]
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
				//var size = Control.Image.Size;
				//UIGraphics.BeginImageContextWithOptions(size, false, 0);
				//var context = UIGraphics.GetCurrentContext();
				//context.DrawImage(new CGRect(0, 0, size.Width, size.Height), Control.Image.CGImage);
				//var image = UIGraphics.GetImageFromCurrentImageContext();
				//Control.Image = image;
			}
		}
	}
}
