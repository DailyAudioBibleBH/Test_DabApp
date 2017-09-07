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
			if (Control == null) return;
			var c = (Android.Widget.Switch)Control;
			c.SetHighlightColor(((Color)App.Current.Resources["HighlightColor"]).ToAndroid());
		}
	}
}
