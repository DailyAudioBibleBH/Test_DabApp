using System;
using Xamarin.Forms;

namespace DABApp
{
	public class DabEditor : Editor
	{
//Code found here:https://solidbrain.com/2017/07/10/placeholder-text-in-xamarin-forms-editor/
		public static BindableProperty PlaceholderProperty
		   = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(DabEditor));

		public static BindableProperty PlaceholderColorProperty
			= BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(DabEditor), Color.Gray);

		public string Placeholder
		{
			get { return (string)GetValue(PlaceholderProperty); }
			set { SetValue(PlaceholderProperty, value); }
		}

		public Color PlaceholderColor
		{
			get { return (Color)GetValue(PlaceholderColorProperty); }
			set { SetValue(PlaceholderColorProperty, value); }
		}
	}
}
