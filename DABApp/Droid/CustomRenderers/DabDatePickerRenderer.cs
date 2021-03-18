using System;
using Android.Content;
using DABApp.DabViewHelpers.Controls;
using DABApp.Droid.CustomRenderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomDatePicker), typeof(DabDatePickerRenderer))]
namespace DABApp.Droid.CustomRenderers
{
    public class DabDatePickerRenderer : DatePickerRenderer
    {
        public DabDatePickerRenderer(Context context) : base(context)
        { }
    }
}
