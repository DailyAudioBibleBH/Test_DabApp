using System;
using System.Diagnostics;
using DABApp;
using DABApp.iOS;
using MediaPlayer;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(AudioOutputView), typeof(ColoredMPVolumeView))]
namespace DABApp.iOS
{
	public class ColoredMPVolumeView : ViewRenderer
	{

		protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.View> e)
		{
			try
			{
				MPVolumeView control = new MPVolumeView();
				control.ShowsVolumeSlider = false;
				UIImage image = new UIImage();
				image = UIImage.FromBundle("airplay");
				control.SetRouteButtonImage(image, UIControlState.Normal);
				control.SetRouteButtonImage(image, UIControlState.Highlighted);
				control.SetRouteButtonImage(image, UIControlState.Disabled);
				control.SetRouteButtonImage(image, UIControlState.Selected);
				SetNativeControl(control);
			}
			catch (Exception ex) 
			{
				Debug.WriteLine(ex.Message);
			}
		}

	}
}
