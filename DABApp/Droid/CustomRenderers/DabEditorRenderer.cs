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
			base.OnElementChanged(e);

			if (Element == null)
				return;

			var element = (DabEditor)Element;

			Control.Hint = element.Placeholder;
			Control.SetHintTextColor(element.PlaceholderColor.ToAndroid());
		}
	}
}
