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
using Android.AccessibilityServices;
using Android.Views.Accessibility;

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
            base.ImportantForAccessibility = ImportantForAccessibility.NoHideDescendants;
            Control.ImportantForAccessibility = ImportantForAccessibility.NoHideDescendants;
            Control.AccessibilityLiveRegion = AccessibilityLiveRegion.None;
            Control.SetAccessibilityDelegate(new ImageDelegate());
        }
    }

    class ImageDelegate : Android.Views.View.AccessibilityDelegate
    {
        public override void OnPopulateAccessibilityEvent(Android.Views.View host, AccessibilityEvent e)
        {
            return;
        }

        public override bool DispatchPopulateAccessibilityEvent(Android.Views.View host, AccessibilityEvent e)
        {
            if (DabImageRenderer.Host != null)
            {
                e.SetSource(DabImageRenderer.Host);
                return base.DispatchPopulateAccessibilityEvent(DabImageRenderer.Host, e);
            }
            return base.DispatchPopulateAccessibilityEvent(host, e);
        }
    }
}