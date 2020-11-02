using System;
using Android.Content;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Button), typeof(DabButtonRenderer))]
namespace DABApp.Droid
{
	public class DabButtonRenderer: ButtonRenderer
	{
		public DabButtonRenderer(Context context) : base(context)
		{
		}

		protected override void OnDraw(Android.Graphics.Canvas canvas)
		{
			base.OnDraw(canvas);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);
		}
	}
}
