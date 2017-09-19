﻿using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ProgressBar), typeof(DabProgressBarRenderer))]
namespace DABApp.Droid
{
	public class DabProgressBarRenderer: ProgressBarRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<ProgressBar> e)
		{
			base.OnElementChanged(e);
			Control.ProgressDrawable.SetTint(((Xamarin.Forms.Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid());
			Control.ProgressDrawable.SetBounds(0, 0, 0, 0);
		}
	}
}