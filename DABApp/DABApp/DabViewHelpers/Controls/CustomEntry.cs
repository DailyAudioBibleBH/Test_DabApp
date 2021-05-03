using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DABApp.DabViewHelpers.Controls
{
    public class CustomEntry : Entry
    {
		public static BindableProperty PlaceholderProperty
		   = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(CustomEntry));

		public static BindableProperty PlaceholderColorProperty
			= BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(CustomEntry), Color.Gray);

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
