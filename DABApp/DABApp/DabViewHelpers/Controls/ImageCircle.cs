using System;
using Xamarin.Forms;

namespace DABApp
{
	//Come back to this
	public class ImageCircle : Image
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