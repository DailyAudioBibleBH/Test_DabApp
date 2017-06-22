using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(Button), typeof(ButtonDisabledTextColorRenderer))]
namespace DABApp.iOS
{
	public class ButtonDisabledTextColorRenderer: ButtonRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				Control.SetTitleShadowColor(((Color)App.Current.Resources["PlayerLabelColor"]).ToUIColor(), UIControlState.Disabled);
				Control.SetTitleColor(((Color)App.Current.Resources["HighlightedButtonDisabledTextColor"]).ToUIColor(), UIControlState.Disabled);
			}
		}
	}
}
