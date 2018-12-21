using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DABApp.iOS;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(StackLayout), typeof(DabStackLayoutRenderer))]
namespace DABApp.iOS
{
    public class DabStackLayoutRenderer: VisualElementRenderer<StackLayout>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<StackLayout> e)
        {
            base.OnElementChanged(e);
            BackgroundColor = BackgroundColor;
        }
    }
}