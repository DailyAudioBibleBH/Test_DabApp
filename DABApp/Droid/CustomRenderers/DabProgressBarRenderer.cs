using System;
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
			var ld = (LayerDrawable)Control.ProgressDrawable;
			ld.SetPadding(0, 0, 0, 0);
			var d1 = (ClipDrawable)ld.FindDrawableByLayerId(Resource.Id.progress_horizontal);
			if (d1 != null)
			{
				d1.SetColorFilter(((Xamarin.Forms.Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid(), PorterDuff.Mode.SrcIn);
			}
		}
	}
}
