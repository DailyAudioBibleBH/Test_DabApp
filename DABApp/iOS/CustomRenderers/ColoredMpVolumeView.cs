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
			//base.OnElementChanged(e);
			//if (e.OldElement != null) {
			//	e.OldElement.PropertyChanged -= OnElementPropertyChanged;
			//}
			//if (e.NewElement != null) {
			//	e.NewElement.PropertyChanged += OnElementPropertyChanged;
			//}
			try
			{
				MPVolumeView control = new MPVolumeView();
				control.ShowsVolumeSlider = false;
			UIImage image = new UIImage();
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				image = UIImage.FromFile("ic_airplay_white_2x.png");
			}
			else
			{
				image = UIImage.FromFile("ic_airplay_white.png");
			}
			control.SetRouteButtonImage(image, UIControlState.Normal);
			control.SetRouteButtonImage(image, UIControlState.Highlighted);
			control.SetRouteButtonImage(image, UIControlState.Disabled);
			control.SetRouteButtonImage(image, UIControlState.Selected);
			SetNativeControl(control);
			}
			catch (Exception ex) { }
		}

		//protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		//	base.OnElementPropertyChanged(sender, e);
		//	MPVolumeView control = new MPVolumeView();
		//	control.ShowsVolumeSlider = false;
		//	UIImage image = new UIImage();
		//	if (Device.Idiom == TargetIdiom.Tablet)
		//	{
		//		image = UIImage.FromFile("ic_airplay_white_2x.png");
		//	}
		//	else
		//	{
		//		image = UIImage.FromFile("ic_airplay_white.png");
		//	}
		//	control.SetRouteButtonImage(image, UIControlState.Normal);
		//	control.SetRouteButtonImage(image, UIControlState.Highlighted);
		//	control.SetRouteButtonImage(image, UIControlState.Disabled);
		//	control.SetRouteButtonImage(image, UIControlState.Selected);
		//	SetNativeControl(control);
		//}

	}
}
