using System;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Switch), typeof(DabSwitchRenderer))]
namespace DABApp.Droid
{
	public class DabSwitchRenderer : SwitchRenderer
	{

		protected override void OnElementChanged(ElementChangedEventArgs<Switch> e)
		{
			base.OnElementChanged(e);
			if (e.OldElement != null)
			{
				this.Element.Toggled -= ElementToggled;
			}
			if (Element == null) return;
			if (Control == null) return;
			if (Control.Checked)
			{
				Control.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["HighlightColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
			else 
			{
				Control.ThumbDrawable.SetColorFilter(((Color)App.Current.Resources["TextColor"]).ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcAtop);
			}
			this.Control.CheckedChange += OnCheckChanged;
			this.Element.Toggled += ElementToggled;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.Control.CheckedChange -= this.OnCheckChanged;
				this.Element.Toggled -= this.ElementToggled;
			}

			base.Dispose(disposing);
		
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
			this.Element.IsToggled = this.Control.Checked;
		}

		private void ElementToggled(object sender, ToggledEventArgs e)
		{
			this.Control.Checked = this.Element.IsToggled;
		}
	}
}
