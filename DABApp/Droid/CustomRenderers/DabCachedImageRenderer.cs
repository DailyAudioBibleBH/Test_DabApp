using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FFImageLoading.Forms;
using FFImageLoading.Forms.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using DABApp.Droid;
using DABApp;

[assembly: ExportRenderer(typeof(BackgroundImage), typeof(DabCachedImageRenderer))]
namespace DABApp.Droid
{
    public class DabCachedImageRenderer : CachedImageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            base.OnElementChanged(e);
            Control.Focusable = false;
            Control.Enabled = false;
            Control.ImportantForAccessibility = ImportantForAccessibility.No;
            Control.AccessibilityLiveRegion = AccessibilityLiveRegion.None;
        }
    }
}