using System;
using System.ComponentModel;
using Foundation;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(DABApp.HtmlLabel), typeof(DABApp.iOS.HtmlLabelRenderer))]
namespace DABApp.iOS
{
	public class HtmlLabelRenderer: LabelRenderer
	{
		string unFormattedText;

		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);

			if (Control != null && Element != null && !string.IsNullOrWhiteSpace(Element.Text))
			{
				SetHtml();
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Label.TextProperty.PropertyName)
			{
				if (Control != null && Element != null && !string.IsNullOrWhiteSpace(Element.Text))
				{
					SetHtml();
				}
			}
		}

		void SetHtml()
		{
			nfloat r, g, b, a;
			Control.TextColor.GetRGBA(out r, out g, out b, out a);
			string textColor = string.Format("#{0:X2}{1:X2}{2:X2}", (int)(r * 255.0), (int)(g * 255.0), (int)(b * 255.0));
			var font = Control.Font;
			var fontName = font.Name;
			var fontSize = font.PointSize;
			string tableStyle = "table{color:white}";
			string aStyle = "a{color:grey}";
			string htmlContents = $"<span style=\"font-family:'{fontName}'; color:{textColor}; font-size:{fontSize}\"><style>{tableStyle}</style><style>{aStyle}</style>{Element.Text}</span>";
			var attr = new NSAttributedStringDocumentAttributes();
			var nsError = new NSError();
			attr.DocumentType = NSDocumentType.HTML;

			var myHtmlData = NSData.FromString(htmlContents, NSStringEncoding.Unicode);
			Control.Lines = 0;
			Control.Text = null;
			Control.AttributedText = new NSAttributedString(myHtmlData, attr, ref nsError);
		}
	}
}
