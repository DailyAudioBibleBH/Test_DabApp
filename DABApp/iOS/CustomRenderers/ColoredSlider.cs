﻿using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Slider), typeof(ColoredSlider))]
namespace DABApp.iOS
{
	public class ColoredSlider: SliderRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Slider> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement == null) return;

			if (Control != null) {
				Control.MinimumTrackTintColor = ((Color)App.Current.Resources["HighlightColor"]).ToUIColor();
				Control.MaximumTrackTintColor = ((Color)App.Current.Resources["HighlightColor"]).ToUIColor();
			}
		}
	}
}
