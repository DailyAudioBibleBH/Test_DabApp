using System;
using DABApp.DabViewHelpers.Controls;
using DABApp.iOS;
using DABApp.iOS.CustomRenderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(DatePicker), typeof(DabDatePickerRenderer))]
namespace DABApp.iOS
{
    class DabDatePickerRenderer : DatePickerRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null && Control != null)
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
                {
                    UIDatePicker picker = (UIDatePicker)Control.InputView;
                    picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;
                }
            }
        }
    }
}
