using System;
using CoreGraphics;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(Entry), typeof(DabEntryRenderer))]
namespace DABApp.iOS
{
	public class DabEntryRenderer: EntryRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);
			Control.LeftView = new UIView(new CGRect(2, 0, 2, 0));
			Control.LeftViewMode = UITextFieldViewMode.Always;
			Control.RightView = new UIView(new CGRect(2, 0, 2, 0));
			Control.RightViewMode = UITextFieldViewMode.Always;
		}
	}
}
