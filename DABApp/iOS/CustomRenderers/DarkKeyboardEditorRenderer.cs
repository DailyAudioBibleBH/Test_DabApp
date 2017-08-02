﻿using System;
using DABApp;
using DABApp.iOS;
using Foundation;
using MarkdownDeep;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(DarkKeyboardEditor), typeof(DarkKeyboardEditorRenderer))]
namespace DABApp.iOS
{
	public class DarkKeyboardEditorRenderer: EditorRenderer
	{
		static Markdown md;

		static DarkKeyboardEditorRenderer() 
		{
			md = new MarkdownDeep.Markdown();

            
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				Control.KeyboardAppearance = UIKit.UIKeyboardAppearance.Dark;
                //SetHtml();
			}
		}

        public override void DidUpdateFocus(UIKit.UIFocusUpdateContext context, UIKit.UIFocusAnimationCoordinator coordinator)
        {
            base.DidUpdateFocus(context, coordinator);
            this.Control.FlashScrollIndicators();
        }

        

		//protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		//	base.OnElementPropertyChanged(sender, e);

		//	if (Control != null)
		//	{
		//		//SetHtml();
		//	}
		//}

		//void SetHtml()
		//{
		//	nfloat r, g, b, a;
		//	Control.TextColor.GetRGBA(out r, out g, out b, out a);
		//	string textColor = string.Format("#{0:X2}{1:X2}{2:X2}", (int)(r * 255.0), (int)(g * 255.0), (int)(b * 255.0));
		//	var font = Control.Font;
		//	var fontName = font.Name;
		//	var fontSize = font.PointSize;
		//	string htmlContents = $"<span style=\"font-family:'{fontName}'; color:{textColor}; font-size:{fontSize}\">{Element.Text}</span>";
		//	var attr = new NSAttributedStringDocumentAttributes();
		//	var nsError = new NSError();
		//	attr.DocumentType = NSDocumentType.HTML;
		//	var myHtmlData = NSData.FromString(htmlContents, NSStringEncoding.Unicode);
		//	Control.AttributedText = new NSAttributedString(myHtmlData, attr, ref nsError);
		//	//Control.Text = md.Transform(Element.Text);
		//}
	}
}
