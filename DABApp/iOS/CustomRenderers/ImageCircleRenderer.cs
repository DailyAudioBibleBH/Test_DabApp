using System;
using System.ComponentModel;
using System.Diagnostics;
using DABApp;
using DABApp.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(ImageCircle), typeof(ImageCircleRenderer))]
namespace DABApp.iOS
{
	public class ImageCircleRenderer: ImageRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null || Element == null)
				return;

			CreateCircle();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == VisualElement.HeightProperty.PropertyName ||
				e.PropertyName == VisualElement.WidthProperty.PropertyName)
			{
				CreateCircle();
			}
		}

		private void CreateCircle()
		{
			try
			{
				double min = Math.Min(Element.Width, Element.Height);
				Control.Layer.CornerRadius = (float)(min / 2.0);
				Control.Layer.MasksToBounds = false;
				Control.Layer.BorderColor = Color.Transparent.ToCGColor();
				Control.Layer.BorderWidth = 0;
				Control.ClipsToBounds = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Unable to create circle image: " + ex);
		    }
		}
	}
}
