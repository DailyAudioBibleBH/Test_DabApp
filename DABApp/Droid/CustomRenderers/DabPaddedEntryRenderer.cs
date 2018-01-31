using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp;
using DABApp.Droid.CustomRenderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(DarkKeyboardEntry), typeof(DabPaddedEntryRenderer))]
namespace DABApp.Droid.CustomRenderers
{
    public class DabPaddedEntryRenderer: EntryRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                var top = Device.Idiom == TargetIdiom.Tablet ? 20 : 50;
                var bottom = Device.Idiom == TargetIdiom.Tablet ? 0 : 50;
                Control.SetPadding(50, top, 50, bottom);
                Control.SetHintTextColor(((Color)App.Current.Resources["SecondaryTextColor"]).ToAndroid());
            }
        }
    }
}