using System;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(DabEditor), typeof(DabEditorRenderer))]
namespace DABApp.Droid
{
	public class DabEditorRenderer : EditorRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
//Code for this custom renderer found here: https://solidbrain.com/2017/07/10/placeholder-text-in-xamarin-forms-editor/
			base.OnElementChanged(e);

			if (Element == null)
				return;

			var element = (DabEditor)Element;

            if (Control != null)
            {
                Control.Hint = element.Placeholder;
                Control.SetHintTextColor(element.PlaceholderColor.ToAndroid());
            }
		}
	}
}
