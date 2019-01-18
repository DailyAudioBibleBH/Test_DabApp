using System;
using System.Collections.Generic;
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

        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            el = (ConfirmationPicker)Element;

        }

        //private void Control_FocusChange(object sender, EventArgs e)
        //{
        //    if (el.IsFocused)
        //    {
        //        var dialog = new Dialog(Context);
        //        dialog.SetContentView(Resource.Layout.ConfirmationPicker);
        //        var title = (Android.Widget.TextView)dialog.FindViewById(Resource.Id.conPickerTitle);
        //        title.Text = el.Title;
        //        Android.Widget.Button cancel = (Android.Widget.Button)dialog.FindViewById(Resource.Id.button1);
        //        Android.Widget.Button submit = (Android.Widget.Button)dialog.FindViewById(Resource.Id.button2);
        //        Android.Widget.ListView listView = (Android.Widget.ListView)dialog.FindViewById(Resource.Id.lv);
        //        listView.Adapter = new ArrayAdapter<String>(Context, Android.Resource.Layout.SimpleExpandableListItem1, el.Items);
        //        cancel.Click += Cancel_Click;
        //        submit.Click += Submit_Click;
        //        dialog.Show();
        //    }
        //}

        //void Cancel_Click(object sender, EventArgs e)
        //{
        //    el.Unfocus();
        //}

        //void Submit_Click(object sender, EventArgs e)
        //{
        //    el.Submission(sender, e);
        //}
    }
}