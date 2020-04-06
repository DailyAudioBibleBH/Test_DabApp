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
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(PlayerLabel), typeof(DABApp.Droid.DabLabelRenderer))]
namespace DABApp.Droid
{
    public class DabLabelRenderer: LabelRenderer
    {
        public DabLabelRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.Focusable = true;
                Control.FocusableInTouchMode = true;
                Control.Enabled = true;
                Control.ImportantForAccessibility = ImportantForAccessibility.Yes;
                Control.AccessibilityLiveRegion = AccessibilityLiveRegion.Assertive;
                Control.SetAccessibilityDelegate(new LabelDelegate());
            }
        }
    }

    class LabelDelegate : Android.Views.View.AccessibilityDelegate
    {
        public override void OnInitializeAccessibilityEvent(Android.Views.View host, AccessibilityEvent e)
        {
            DabImageRenderer.Host = host;
            base.OnInitializeAccessibilityEvent(host, e);
        }
    }
}