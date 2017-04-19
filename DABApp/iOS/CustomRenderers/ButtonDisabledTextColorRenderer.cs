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
				Control.SetTitleColor(UIColor.FromRGB(187, 187, 187), UIControlState.Focused);
				Control.SetTitleColor(UIColor.FromRGB(187, 187, 187), UIControlState.Selected);
			}
		}
	}
}
