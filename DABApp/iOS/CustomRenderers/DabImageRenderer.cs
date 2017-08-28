using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

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
				var size = Control.Image.Size;
				UIGraphics.BeginImageContextWithOptions(new CoreGraphics.CGSize(30 , 30), false, 0);
				var image = UIGraphics.GetImageFromCurrentImageContext();
				Control.Image = image;
			}
		}
	}
}
