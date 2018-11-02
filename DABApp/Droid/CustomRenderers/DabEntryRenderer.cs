using System;
using Acr.DeviceInfo;
using Android.Content;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Entry), typeof(DabEntryRenderer))]
namespace DABApp.Droid
{
    public class DabEntryRenderer : EntryRenderer
    {
        public DabEntryRenderer(Context context) : base(context)
        { }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                var left = Device.Idiom == TargetIdiom.Tablet ? 25 : 50;
                var right = Device.Idiom == TargetIdiom.Tablet ? 25 : 50;
                Control.SetPadding(left, 0, right, 0);
                Control.Gravity = Android.Views.GravityFlags.CenterVertical;
                Control.SetHintTextColor(((Color)App.Current.Resources["SecondaryTextColor"]).ToAndroid());
            }
        }
    }
}