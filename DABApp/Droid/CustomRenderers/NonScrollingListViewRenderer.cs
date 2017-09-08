using System;
using Xamarin.Forms;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(NonScrollingListView), typeof(NonScrollingListViewRenderer))]
namespace DABApp.Droid
{
	public class NonScrollingListViewRenderer: ListViewRenderer
	{
		

		public override bool DispatchTouchEvent(Android.Views.MotionEvent e)
		{
			if (e.Action == Android.Views.MotionEventActions.Move) {
				return true;
			}
			return base.DispatchTouchEvent(e);
		}
	}
}
