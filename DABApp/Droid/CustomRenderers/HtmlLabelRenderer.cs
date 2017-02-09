using System;
using System.ComponentModel;
using Android.Text;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(DABApp.HtmlLabel), typeof(DABApp.Droid.HtmlLabelRenderer))]
namespace DABApp.Droid
{
	public class HtmlLabelRenderer: LabelRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);

			Control?.SetText(Html.FromHtml(Element.Text), TextView.BufferType.Spannable);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Label.TextProperty.PropertyName)
			{
				Control?.SetText(Html.FromHtml(Element.Text), TextView.BufferType.Spannable);
			}
		}
	}
}
