using System;
using System.Diagnostics;
using Android.Graphics;
using DABApp;
using DABApp.Droid;
using FFImageLoading.Forms.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(ImageCircle), typeof(ImageCircleRenderer))]
namespace DABApp.Droid
{
	public class ImageCircleRenderer: CachedImageRenderer
	{
		protected override bool DrawChild(Canvas canvas, global::Android.Views.View child, long drawingTime)
		{
			try
			{
				var radius = Math.Min(Width, Height) / 2;
				var strokeWidth = 1;
				radius -= strokeWidth / 2;

				//Create path to clip
				var path = new Path();
				path.AddCircle(Width / 2, Height / 2, radius, Path.Direction.Ccw);
				canvas.Save();
				canvas.ClipPath(path);

				var result = base.DrawChild(canvas, child, drawingTime);

				canvas.Restore();

				// Create path for circle border
				//path = new Path();
				//path.AddCircle(Width / 2, Height / 2, radius, Path.Direction.Ccw);

				//var paint = new Paint();
				//paint.AntiAlias = true;
				//paint.StrokeWidth = 5;
				//paint.SetStyle(Paint.Style.Stroke);
				//paint.Color = global::Android.Graphics.Color.White;

				//canvas.DrawPath(path, paint);

				//Properly dispose
				//paint.Dispose();
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
