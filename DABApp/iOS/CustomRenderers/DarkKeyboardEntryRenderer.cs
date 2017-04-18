using System;
using DABApp;
using DABApp.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(DarkKeyboardEntry), typeof(DarkKeyboardEntryRenderer))]
namespace DABApp.iOS
{
	public class DarkKeyboardEntryRenderer: EntryRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);

			if (Control != null) {
				Control.KeyboardAppearance = UIKit.UIKeyboardAppearance.Dark;
			}
		}
	}
}
