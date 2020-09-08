using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DABApp.DabViewHelpers.Controls;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.Droid.DependencyServices;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace DABApp.Droid.DependencyServices
{
    class CustomEntryRenderer : EntryRenderer 
    {
        public CustomEntryRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == null) return;

            // This line do the trick
            Control.FocusChange += Control_FocusChange;
        }

        void Control_FocusChange(object sender, FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                (Forms.Context as Activity).Window.SetSoftInputMode(SoftInput.AdjustResize);
            else
                (Forms.Context as Activity).Window.SetSoftInputMode(SoftInput.AdjustNothing);
        }
    }
}