using System;
using DABApp;
using DABApp.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(DarkKeyboardEditor), typeof(DarkKeyboardEditorRenderer))]
namespace DABApp.iOS
{
	public class DarkKeyboardEditorRenderer: EditorRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				Control.KeyboardAppearance = UIKit.UIKeyboardAppearance.Dark;
			}
		}
	}
}
