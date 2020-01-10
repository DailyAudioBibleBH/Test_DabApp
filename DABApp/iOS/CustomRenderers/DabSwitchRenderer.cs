using System;
using CoreGraphics;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Switch), typeof(DabSwitchRenderer))]
namespace DABApp.iOS
{
	public class DabSwitchRenderer : SwitchRenderer
	{

		protected override void OnElementChanged(ElementChangedEventArgs<Switch> e)
		{
			base.OnElementChanged(e);

            if (Control != null)
            {
                Control.OnTintColor = ((Color)App.Current.Resources["HighlightColor"]).ToUIColor();
            }
        }

	}
}
