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
			if (Control != null)
			{
				var top = Device.Idiom == TargetIdiom.Tablet ? 20 : 50;
				var bottom = Device.Idiom == TargetIdiom.Tablet ? 0 : 50;
				Control.SetPadding(50, top, 50, bottom);
			}
		}
	}
}
