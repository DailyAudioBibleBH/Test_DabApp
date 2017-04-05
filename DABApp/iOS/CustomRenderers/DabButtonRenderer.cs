using System;
using CoreGraphics;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Button), typeof(DabButtonRenderer))]
namespace DABApp.iOS
{

	public class DabButtonRenderer : ButtonRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);

			//Match the tint color to the button text color
			Control.TintColor = Control.CurrentTitleColor;
		}

	}
}
