using System;
using CoreGraphics;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Picker), typeof(DabPickerRenderer))]
namespace DABApp.iOS
{

	public class DabPickerRenderer : PickerRenderer
	{

        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            //Set the font size
            Control.Font = UIFont.SystemFontOfSize((nfloat)Device.GetNamedSize(NamedSize.Medium, typeof(Picker)));

            //Set custom padding
            Control.LeftView = new UIView(new CGRect(0, 0, 10, 0));
            Control.LeftViewMode = UITextFieldViewMode.Always;
            //Remove the border
            Control.BorderStyle = UITextBorderStyle.None;
            Control.ClipsToBounds = true;
            Control.Layer.CornerRadius = 5.0f;
            //Control.TextColor = ((Color)App.Current.Resources["SecondaryTextColor"]).ToUIColor();
            //Update the tint color to match whatever text color we're using.

            //Add an icon to the right side of the element
            Control.RightViewMode = UITextFieldViewMode.Always;
            UIImage icon = UIImage.FromBundle("down_arrow");
            icon = icon.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            Control.RightView = new UIImageView(icon);
            if (Control.Enabled == false)
            {
                Control.TextColor = ((Color)App.Current.Resources["ActivityHolderBackground"]).ToUIColor();
            }
            Control.TintColor = Control.TextColor;
        }
    }
}
