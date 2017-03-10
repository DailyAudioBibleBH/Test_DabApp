using System;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(Entry), typeof(DabEntryRenderer))]
namespace DABApp.Droid
{
	public class DabEntryRenderer: EntryRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);
			Control.SetPadding(50, 50, 50, 50);
		}
	}
}
