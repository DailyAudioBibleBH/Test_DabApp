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
        public bool onRecord { get; set; } = false;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var m = TimeSpan.FromSeconds((double)value);
			var r = new TimeSpan(m.Days, m.Hours, m.Minutes, m.Seconds, 0);
            if(r.Hours == 0)
			{
                if (onRecord)
                {
                    return m.ToString(@"m\:ss");
                }
                else
                {
                    return $"{r.Minutes:D2}:{r.Seconds:D2}";
                }
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

	public class CardConverter : IValueConverter 
	{ 
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var card = (Card)value;
			return $"{card.brand} ending in {card.last4}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class HistoryConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null;}

			var history = (DonationRecord)value;
			return $"{history.campaignName}-{history.currency}{history.grossAmount}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class ParticipantConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null; }

			var topic = (Topic)value;
			return $"Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
	
		}		
	}

	public class ActivityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null; }
	
			var topic = (Topic)value;
			return $"Latest Reply {topic.lastActivity}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();

		}	
	}

	public class ReplyConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return null;
			var reply = (Member)value;
			return $"Prayers:{reply.replyCount}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class TopicConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return null;
			var topic = (Member)value;
			return $"Topics:{topic.topicCount}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class TimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return null;
			var res = (Reply)value;
			var dateTime = DateTimeOffset.Parse($"{res.gmtDate} +0:00").UtcDateTime.ToLocalTime();
			string month = dateTime.ToString("MMMM");
			string time = dateTime.ToString("t");
			return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class InverseConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var val = (bool)value;
			return !val;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
