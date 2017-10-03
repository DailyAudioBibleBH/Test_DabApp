using System;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Switch), typeof(DabSwitchRenderer))]
namespace DABApp.Droid
{
	public class DabSwitchRenderer : SwitchRenderer
	{
		protected override void Dispose(bool disposing)
		{
			Control.CheckedChange -= this.OnCheckChanged;
			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Switch> e)
		{
			base.OnElementChanged(e);
			if (Control == null) return;
			if (Control.Checked)
			{
				Control.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["HighlightColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
			else 
			{
				Control.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["TextColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
			Control.CheckedChange += OnCheckChanged;
		}

		private void OnCheckChanged(object sender, Android.Widget.CompoundButton.CheckedChangeEventArgs e) 
		{
			if (Control.Checked)
			{
				Control.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["HighlightColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
			else 
			{
				Control.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["TextColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
		}
	}
}
