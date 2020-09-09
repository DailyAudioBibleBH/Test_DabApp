using System;
using System.Diagnostics;
using Android.Content;
using Android.Graphics;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(ImageCircle), typeof(ImageCircleRenderer))]
namespace DABApp.Droid
{
	public class ImageCircleRenderer: ImageRenderer
	{
        //Come back to this
        public ImageCircleRenderer(Context context) : base(context)
		{

		}
		protected override bool DrawChild(Canvas canvas, global::Android.Views.View child, long drawingTime)
		{
			try
			{
				var radius = Math.Min(Width, Height) / 2;
				var strokeWidth = 1;
				radius -= strokeWidth / 2;

				//Create path to clip
				var path = new Path();
				var color = (Xamarin.Forms.Color)App.Current.Resources["HighlightColor"];
				path.AddCircle(Width / 2, Height / 2, radius, Path.Direction.Ccw);
				canvas.Save();
				canvas.ClipPath(path);
				canvas.DrawRGB(Convert.ToInt32(color.R*255), Convert.ToInt32(color.G*255), Convert.ToInt32(color.B*255));
				var result = base.DrawChild(canvas, child, drawingTime);

				canvas.Restore();


				path.Dispose();
				return result;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Unable to create circle image: " + ex);
			}

			return base.DrawChild(canvas, child, drawingTime);
		}

		//protected override void OnElementPropertyChanged(ElementChangedEventArgs<Image> e)
		//{
		//	base.OnElementChanged(e);

		//	if (e.OldElement == null)
		//	{

		//		if ((int)Android.OS.Build.VERSION.SdkInt < 18)
		//			SetLayerType(Android.Views.LayerType.Software, null);
		//	}
		//}
	}
}
