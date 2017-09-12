using System;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ProgressBar), typeof(DabProgressBarRenderer))]
namespace DABApp.Droid
{
	public class DabProgressBarRenderer: ProgressBarRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<ProgressBar> e)
		{
			base.OnElementChanged(e);
			if (Control != null)
			{
			}
		}
	}
}
