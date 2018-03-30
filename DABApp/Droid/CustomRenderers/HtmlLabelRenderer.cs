using System;
using System.ComponentModel;
using Android.Content;
using Android.Text;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(DABApp.HtmlLabel), typeof(DABApp.Droid.HtmlLabelRenderer))]
namespace DABApp.Droid
{
	public class HtmlLabelRenderer: LabelRenderer
	{
        public HtmlLabelRenderer(Context context) : base (context)
        { }

		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);
			Control.SetMaxLines(100000);

			if (Element.Text != null)
			{
				Control?.SetText(Html.FromHtml(Element.Text), TextView.BufferType.Spannable);
			}
            Control?.SetTextIsSelectable(((HtmlLabel)Element).IsSelectable);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Label.TextProperty.PropertyName)
			{
				if (Element.Text != null)
				{
					Control?.SetText(Html.FromHtml(Element.Text), TextView.BufferType.Spannable);
				}
			}
            Control?.SetTextIsSelectable(((HtmlLabel)Element).IsSelectable);
		}
	}
}
