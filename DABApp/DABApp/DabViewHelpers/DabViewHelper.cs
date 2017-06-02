using System;
using System.Globalization;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public static class DabViewHelper
	{
		public static void InitDabForm(DabBaseContentPage page)
		{
			page.Title = "DAILY AUDIO BIBLE";
		}
	}

	public class StringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var r = TimeSpan.FromSeconds((double)value);
            if(r.Hours == 0)
			{
				return $"{r.Minutes:D2}:{r.Seconds:D2}";
			}
			else { 
				return $"{r.Hours:D2}:{r.Minutes:D2}:{r.Seconds:D2}";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
