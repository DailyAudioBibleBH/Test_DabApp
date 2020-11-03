using System;
using System.Runtime.CompilerServices;
using Android.Widget;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly:ExportRenderer(typeof(NavigationPage), typeof(DabNavigationPageRenderer))]
namespace DABApp.Droid
{
	public class DabNavigationPageRenderer : NavigationPageRenderer
	{
		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);
			ConfigureActionBar();
		}

		void ConfigureActionBar()
		{
			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
			if (toolbar.ChildCount > 0)
			{
				var item = toolbar.GetChildAt(0);
			}
		}
	}
}
