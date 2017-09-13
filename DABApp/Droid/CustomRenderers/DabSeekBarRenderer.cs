using System;
using Android.Graphics;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(DabSeekBar), typeof(DabSeekBarRenderer))]
namespace DABApp.Droid
{
	public class DabSeekBarRenderer: SliderRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Slider> e)
		{
			base.OnElementChanged(e);
			Control.Thumb.SetTint(Android.Graphics.Color.White);
			Control.ProgressDrawable.SetTint(Android.Graphics.Color.White);
		}
	}
}
