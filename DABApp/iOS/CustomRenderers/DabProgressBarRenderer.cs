using System;
using CoreGraphics;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ProgressBar), typeof(DabProgressBarRenderer))]
namespace DABApp.iOS
{
	public class DabProgressBarRenderer : ProgressBarRenderer
	{

		protected override void OnElementChanged(ElementChangedEventArgs<ProgressBar> e)
		{
			base.OnElementChanged(e);

			Control.ProgressTintColor = ((Color)App.Current.Resources["PlayerLabelColor"]).ToUIColor();
			Control.TrackTintColor = ((Color)App.Current.Resources["NonScrollingListViewColor"]).ToUIColor();
		}
	}
}
