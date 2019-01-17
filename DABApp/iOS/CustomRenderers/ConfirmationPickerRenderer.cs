using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DABApp;
using DABApp.iOS.CustomRenderers;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(ConfirmationPicker), typeof(ConfirmationPickerRenderer))]
namespace DABApp.iOS.CustomRenderers
{
    public class ConfirmationPickerRenderer: PickerRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                UIToolbar toolbar = new UIToolbar();
                toolbar.SizeToFit();
                toolbar.BarStyle = UIBarStyle.Default;
                UIBarButtonItem title = new UIBarButtonItem();
                title.Title = "Select podcast";
                UIBarButtonItem cancel = new UIBarButtonItem();
                cancel.Title = "Cancel";
                var el = (ConfirmationPicker)Element;
                cancel.Clicked += (sender, EventArgs) => { el.Unfocus(); };
                UIBarButtonItem submit = new UIBarButtonItem();
                submit.Title = "Submit";
                submit.Clicked += (sender, EventArgs) => { el.Submission(sender, EventArgs); };
                toolbar.SetItems(new UIBarButtonItem[] { title, cancel, submit }, true);
                Control.InputAccessoryView = toolbar;
            }
        }
    }
}