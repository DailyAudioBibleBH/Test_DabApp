using System;
using DABApp;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(DabSeekBar), typeof(ColoredSlider))]
namespace DABApp.iOS
{
	public class ColoredSlider : SliderRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Slider> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement == null) return;

			if (Control != null)
			{
				Control.MaximumTrackTintColor = ((Color)App.Current.Resources["NonScrollingListViewColor"]).ToUIColor();
				Control.MinimumTrackTintColor = ((Color)App.Current.Resources["PlayerLabelColor"]).ToUIColor();
				Control.SetThumbImage(UIImage.FromFile("seekbaricon.png"), UIControlState.Normal);
				var element = (DabSeekBar)e.NewElement;
				Control.AllTouchEvents += (sender, er) => {
					element.Touched(sender, er);
				};
			}
		}
	}
}