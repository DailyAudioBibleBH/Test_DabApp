using System;
using System.ComponentModel;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(DABApp.HtmlLabel), typeof(DABApp.iOS.HtmlLabelRenderer))]
namespace DABApp.iOS
{
    public class HtmlLabelRenderer : ViewRenderer<Label, UITextView>
    {
        string unFormattedText;
        UITextView field;

		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);
            if (Control == null)
            {
                field = new UITextView();
                field.ScrollEnabled = false;
                SetNativeControl(field);
                if (Control != null)
                {
                    Control.Editable = false;
                }
                if (Element != null)
                {
                    Control.UserInteractionEnabled = ((HtmlLabel)Element).IsSelectable;
                }
            }

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
                    Control.UserInteractionEnabled = ((HtmlLabel)Element).IsSelectable;
				}
			}
		}

		void SetHtml()
		{
            //nfloat r, g, b, a;
            var label = new UILabel();
            var color = Element.TextColor;
			string textColor = string.Format("#{0:X2}{1:X2}{2:X2}", (int)(color.R * 255.0), (int)(color.G * 255.0), (int)(color.B * 255.0));
            UIStringAttributes fontName = new UIStringAttributes();  //Grabbing correct font after iOS 13 update
            fontName.Font = UIKit.UIFont.PreferredBody;
            var fontSize = Element.FontSize;
            string tableStyle = "table{color:white}";
            string aStyle = "a{color:grey}";
            string htmlContents = $"<span style=\"font-family:'{fontName.Font.FamilyName}'; color:{textColor}; font-size:{fontSize}\"><style>{tableStyle}</style><style>{aStyle}</style>{Element.Text}</span>";
            var attr = new NSAttributedStringDocumentAttributes();
            var nsError = new NSError();
            attr.DocumentType = NSDocumentType.HTML;

            var myHtmlData = NSData.FromString(htmlContents, NSStringEncoding.Unicode);
			//Control.Lines = 0;
			//Control.Text = null;
            Control.BackgroundColor = Element.BackgroundColor.ToUIColor();
			Control.AttributedText = new NSAttributedString(myHtmlData, attr, ref nsError);
		}
	}
}
