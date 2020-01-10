using System;
using DABApp;
using DABApp.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(NonScrollingFlowListView), typeof(NonScrollingFlowListViewRenderer))]
namespace DABApp.iOS
{
	public class NonScrollingFlowListViewRenderer: ListViewRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<ListView> e)
		{
			base.OnElementChanged(e);
            if (Control != null)
            {
                Control.ScrollEnabled = false;
            }
        }
	}
}
