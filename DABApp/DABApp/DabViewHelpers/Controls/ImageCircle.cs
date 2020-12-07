using System;
using FFImageLoading.Forms;
using Xamarin.Forms;

namespace DABApp
{
	public class ImageCircle : CachedImage
	{
		public ImageCircle()
		{
			if (Device.RuntimePlatform == "iOS")
			{
				BackgroundColor = (Color)App.Current.Resources["HighlightColor"];
			}
		}
	}
}
