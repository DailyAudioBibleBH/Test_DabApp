using System;
using Foundation;
using UIKit;
using DABApp.iOS;
using CoreGraphics;

[assembly: Xamarin.Forms.Dependency(typeof(KeyboardHelper))]
namespace DABApp.iOS
{
	// Raises keyboard changed events containing the keyboard height and
	// whether the keyboard is becoming visible or not
	public class KeyboardHelper : IKeyboardHelper
	{
		public KeyboardHelper()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardNotification);
			NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardNotification);
		}

		public event EventHandler<KeyboardHelperEventArgs> KeyboardChanged;

		private void OnKeyboardNotification(NSNotification notification)
		{
			var standardKeyboardHeight = Xamarin.Forms.Device.Idiom == Xamarin.Forms.TargetIdiom.Phone ? 44 : 55;
			var userInfo = notification.UserInfo;
			var keyEnd = (NSValue)userInfo.ValueForKey(UIKeyboard.FrameEndUserInfoKey);
			var keyBegin = (NSValue)userInfo.ValueForKey(UIKeyboard.FrameBeginUserInfoKey);
			var diff = keyEnd.CGRectValue.Y - keyBegin.CGRectValue.Y;
			var height = UIScreen.MainScreen.Bounds.Height;
			var visible = notification.Name == UIKeyboard.WillShowNotification;
			var keyboardFrame = visible
				? UIKeyboard.FrameEndFromNotification(notification)
				: UIKeyboard.FrameBeginFromNotification(notification);
			var isExternal = Math.Abs(diff) == standardKeyboardHeight || diff > standardKeyboardHeight;
			if (KeyboardChanged != null)
			{
				KeyboardChanged(this, new KeyboardHelperEventArgs(visible, (float)keyboardFrame.Height, isExternal));
			}
		}
	}
}
