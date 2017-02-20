using System;
using Xamarin.Forms;
using DABApp;
using DABApp.iOS;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(NonScrollingListView), typeof(NonScrollingListViewRenderer))]
namespace DABApp.iOS
{
	public class NonScrollingListViewRenderer: ListViewRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<ListView> e)
		{
			base.OnElementChanged(e);
			Control.ScrollEnabled = false;
		}
	}
}
