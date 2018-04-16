using System;
using Android.Content;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Picker), typeof(DabPickerRenderer))]
namespace DABApp.Droid
{
    public class DabPickerRenderer : PickerRenderer
    {
        public DabPickerRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);
            var button = e.NewElement;

            if (this.Control != null)
            {
                try
                {
                    this.Control.Background = Resources.GetDrawable(Resource.Drawable.down_arrow);
                }
                catch (Exception ex) { }
            }
        }

    }
}
