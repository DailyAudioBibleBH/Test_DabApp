using System;
using SegmentedControl.FormsPlugin.Android;
using SegmentedControl.FormsPlugin.Abstractions;
using Xamarin.Forms;
using DABApp.Droid;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(SegmentedControl.FormsPlugin.Abstractions.SegmentedControl), typeof(DabSegmentedControlRenderer))]
namespace DABApp.Droid
{
	public class DabSegmentedControlRenderer: SegmentedControlRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<SegmentedControl.FormsPlugin.Abstractions.SegmentedControl> e)
		{
			base.OnElementChanged(e);
		}

		protected override void OnFocusChanged(bool gainFocus, Android.Views.FocusSearchDirection direction, Android.Graphics.Rect previouslyFocusedRect)
		{
			base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
		}
	}
}
