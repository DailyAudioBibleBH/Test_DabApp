using System;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly:ExportRenderer(typeof(NavigationPage), typeof(DabNavigationPageRenderer))]
namespace DABApp.Droid
{
	public class DabNavigationPageRenderer: NavigationPageRenderer
	{
		public DabNavigationPageRenderer()
		{
		}

		public override void OnViewAdded(Android.Views.View child)
		{
			base.OnViewAdded(child);
		}
	}
}
