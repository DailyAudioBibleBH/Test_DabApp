using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly:ExportRenderer(typeof(ConfirmationPicker), typeof(ConfirmationPickerRenderer))]
namespace DABApp.Droid.CustomRenderers
{
    public class ConfirmationPickerRenderer: Xamarin.Forms.Platform.Android.PickerRenderer
    {
        ConfirmationPicker el;

        public ConfirmationPickerRenderer(Context context): base(context)
        {
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            el = (ConfirmationPicker)Element;

            if (e.PropertyName == "SelectedIndex")
            {
                el.Submission(sender, e);
            }
        }
    }
}