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
			var m = TimeSpan.FromSeconds((double)value);
			var r = new TimeSpan(m.Days, m.Hours, m.Minutes, m.Seconds, 0);
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
			return $"Voices: {topic.voiceCount}  Replies: {topic.replyCount}";
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
			return $"Latest Reply {topic.lastActivity} ago";
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
			return $"Replies:{reply.replyCount}";
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
			var dateTime = System.Convert.ToDateTime(res.gmtDate);
			string month = dateTime.ToString("MMMM");
			string time = dateTime.TimeOfDay.ToString();
			return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
