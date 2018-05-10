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

[assembly: ExportRenderer(typeof(PlayerImage), typeof(DabImageRenderer))]
namespace DABApp.Droid.CustomRenderers
{
    public class DabImageRenderer : ImageRenderer
    {
        public DabImageRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<Image> e)
        {
            base.OnElementChanged(e);
            Control.Focusable = true;
            Control.ImportantForAccessibility = ImportantForAccessibility.Yes;
            Control.AccessibilityLiveRegion = AccessibilityLiveRegion.Assertive;
        }
    }
}