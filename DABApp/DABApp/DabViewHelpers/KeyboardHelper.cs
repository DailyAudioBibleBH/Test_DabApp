using System;
using Xamarin.Forms;

namespace DABApp
{
	// Provides static access to keyboard events
	public static class KeyboardHelper
	{
		private static IKeyboardHelper keyboardHelper = null;

		public static void Init()
		{
			if (keyboardHelper == null)
			{
				keyboardHelper = DependencyService.Get<IKeyboardHelper>();
			}
		}

		public static event EventHandler<KeyboardHelperEventArgs> KeyboardChanged
		{
			add
			{
				Init();
				keyboardHelper.KeyboardChanged += value;
			}
			remove
			{
				Init();
				keyboardHelper.KeyboardChanged -= value;
			}
		}
	}

	public interface IKeyboardHelper
	{
		event EventHandler<KeyboardHelperEventArgs> KeyboardChanged;
	}

	public class KeyboardHelperEventArgs : EventArgs
	{
		public readonly bool Visible;
		public readonly float Height;
		public readonly bool IsExternalKeyboard;

		public KeyboardHelperEventArgs(bool visible, float height, bool isExternalKeyboard)
		{
			Visible = visible;
			Height = height;
			IsExternalKeyboard = isExternalKeyboard;
		}
	}
}
