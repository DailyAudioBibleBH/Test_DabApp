using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(PlayerImage), typeof(DabImageRenderer))]
namespace DABApp.Droid
{
    public class DabImageRenderer : ImageRenderer
    {
        public static Android.Views.View Host { get; set; }

        public DabImageRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<Image> e)
        {
            base.OnElementChanged(e);
            Control.Focusable = true;
            Control.FocusableInTouchMode = true;
            Control.Enabled = true;
            Control.ImportantForAccessibility = ImportantForAccessibility.Yes;
            Control.AccessibilityLiveRegion = AccessibilityLiveRegion.Assertive;
            Control.SetAccessibilityDelegate(new DabAccessibilityDelegate());
        }
    }

    class DabAccessibilityDelegate : Android.Views.View.AccessibilityDelegate
    {
        public override void OnInitializeAccessibilityEvent(Android.Views.View host, AccessibilityEvent e)
        {
            DabImageRenderer.Host = host;
            base.OnInitializeAccessibilityEvent(host, e);
        }
    }
}