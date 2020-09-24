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
                title.Title = "Select channel";
                title.Style = UIBarButtonItemStyle.Plain;
                title.Enabled = false;
                var l = new UITextAttributes();
                l.TextColor = UIColor.Black;
                title.SetTitleTextAttributes(l, UIControlState.Normal);
                UIBarButtonItem cancel = new UIBarButtonItem();
                cancel.Title = "Cancel";
                var el = (ConfirmationPicker)Element;
                cancel.Style = UIBarButtonItemStyle.Done;
                cancel.Clicked += (sender, EventArgs) => { el.Unfocus(); };
                UIBarButtonItem submit = new UIBarButtonItem();
                submit.Title = "Submit";
                submit.Style = UIBarButtonItemStyle.Done;
                EventHandler eh = null;
                UIBarButtonItem flexibleSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, eh);
                submit.Clicked += (sender, EventArgs) => { el.Submission(sender, EventArgs); };
                toolbar.SetItems(new UIBarButtonItem[] { title, flexibleSpace, cancel, submit }, true);
                Control.InputAccessoryView = toolbar;
            }
        }
    }
}