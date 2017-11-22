using System;
using DABApp;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(DabEditor), typeof(DabEditorRenderer))]
namespace DABApp.iOS
{
	public class DabEditorRenderer : EditorRenderer
	{
		private UILabel _placeholderLabel;

		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
//Code Found here: https://solidbrain.com/2017/07/10/placeholder-text-in-xamarin-forms-editor/
			base.OnElementChanged(e);

			if (Element == null)
				return;

			CreatePlaceholderLabel((DabEditor)Element, Control);

			Control.Ended += OnEnded;
			Control.Changed += OnChanged;
		}

		private void CreatePlaceholderLabel(DabEditor element, UITextView parent)
		{
			_placeholderLabel = new UILabel
			{
				Text = element.Placeholder,
				TextColor = element.PlaceholderColor.ToUIColor(),
				BackgroundColor = UIColor.Clear,
				//Font = UIFont.FromName(element.FontFamily, (nfloat)element.FontSize)
			};
			_placeholderLabel.SizeToFit();

			parent.AddSubview(_placeholderLabel);

			parent.LayoutIfNeeded();

			_placeholderLabel.Hidden = parent.HasText;
		}

		private void OnEnded(object sender, EventArgs args)
		{
			if (!((UITextView)sender).HasText && _placeholderLabel != null)
				_placeholderLabel.Hidden = false;
		}

		private void OnChanged(object sender, EventArgs args)
		{
			if (_placeholderLabel != null)
				_placeholderLabel.Hidden = ((UITextView)sender).HasText;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Control.Ended -= OnEnded;
				Control.Changed -= OnChanged;

				_placeholderLabel?.Dispose();
				_placeholderLabel = null;
			}

			base.Dispose(disposing);
		}
	}
}
