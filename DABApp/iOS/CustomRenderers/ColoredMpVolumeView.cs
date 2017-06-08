using System;
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
			base.OnElementChanged(e);

			MPVolumeView control = new MPVolumeView();
			control.ShowsVolumeSlider = false;
			UIImage image = UIImage.FromFile("ic_airplay_white.png");
			control.SetRouteButtonImage(image, UIControlState.Normal);
			control.SetRouteButtonImage(image, UIControlState.Highlighted);
			SetNativeControl(control);
		}

	}
}
