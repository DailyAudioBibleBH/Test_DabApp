using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DABApp.DabSockets;
using SlideOverKit;
using SQLite;
using Xamarin.Forms;
using static DABApp.ContentConfig;

namespace DABApp
{
	public static class DabViewHelper
	{
		public static void InitDabForm(DabBaseContentPage page)
		{
			if (GlobalResources.TestMode)
			{
				page.Title = "*** TEST MODE ***";
			}
			else
			{
				page.Title = "DAILY AUDIO BIBLE";
			}
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
			var card = (dbCreditCards)value;
            if (card.cardStatus == "NewCardFunction")
            {
				return $"{card.cardType}";
			}
			return $"{card.cardType} ending in {card.cardLastFour}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class CardLastFourConverter : IValueConverter
	{
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null; }
			//TODO: Figure how to tie history to credit card
			dbDonationHistory history = (dbDonationHistory)value;
			dbCreditCards card = adb.Table<dbCreditCards>().FirstOrDefaultAsync().Result;
			string cardNumber;
			switch (card.cardType)
			{
				case "American Express":
					cardNumber = $"**** ****** *{card.cardLastFour}";
					break;
				default:
					cardNumber = $"**** **** **** {card.cardLastFour}";
					break;
			}
			return $"{cardNumber}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class DateConverter : IValueConverter
    {
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null; }

			dbDonationHistory history = (dbDonationHistory)value;
			string date = history.historyDate.ToString("M/dd/yyyy", CultureInfo.InvariantCulture);
			return $"{date}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class HistoryConverter : IValueConverter
	{
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null;}

			var history = (dbDonationHistory)value;
			int hisCampWpId = history.historyCampaignWpId;
			string campaignName = adb.Table<dbCampaigns>().Where(x => x.campaignWpId == hisCampWpId).FirstOrDefaultAsync().Result.campaignTitle;
			string symbol;
			if (!TryGetCurrencySymbol(history.historyCurrency, out symbol))
			{
				symbol = history.historyCurrency;
			};
			return $"{campaignName} - {symbol}{GlobalResources.ToCurrency(history.historyGrossDonation)}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public bool TryGetCurrencySymbol(string ISOCurrencySymbol, out string symbol)
		{
			symbol = CultureInfo
				.GetCultures(CultureTypes.AllCultures)
				.Where(c => !c.IsNeutralCulture)
				.Select(culture => {
					try
					{
						return new RegionInfo(culture.Name);
					}
					catch
					{
						return null;
					}
				})
				.Where(ri => ri != null && ri.ISOCurrencySymbol == ISOCurrencySymbol)
				.Select(ri => ri.CurrencySymbol)
				.FirstOrDefault();
			return symbol != null;
		}
	}

	public class ParticipantConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null; }

			var topic = (DabGraphQlTopic)value;
			DateTime date = topic.createdAt.ToLocalTime();
			string createdAt = TimeConvert(date);
			return $"By {topic.userNickname}  @ {createdAt}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
	
		}

		string TimeConvert(DateTime createdAt)
		{
			var dateTime = createdAt.ToLocalTime();
			var month = dateTime.ToString("MMMM");
			var time = dateTime.ToString("t");
			TimeSpan ts = (DateTime.Now - dateTime);

			if (ts.TotalDays >1)
            {
				return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
			}
            else
            {
				return time;
            }
		}
	}

	public class ActivityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) { return null; }

			var topic = (DabGraphQlTopic)value;
			DateTime lastActive = topic.lastActive.ToLocalTime();
			TimeSpan ts = (DateTime.Now - lastActive);
            if (ts.TotalDays >= 1)
            {
				return $"Latest Reply: {ts.Days} days {ts.Hours} hours {ts.Minutes} minutes ago,  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else if (ts.TotalDays >= 1)
            {
				return $"Latest Reply: {ts.Hours} hours {ts.Minutes} minutes ago,  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else if (ts.TotalMinutes >= 1)
            {
				return $"Latest Reply: {ts.Minutes} minutes ago,  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else
            {
				return $"Latest Reply: Just now,  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
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
            try
            {
                if (value == null) return null;
                var reply = (DabGraphQlTopic)value;
                return $"Prayers: {reply.replyCount}";
            }
            catch (Exception ex)
            {
				return null;
            }
			
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class ReplyPrayerConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				if (value == null) return null;
				var reply = (DabGraphQlReply)value;
				return $"Prayers: {reply.userReplies}";
			}
			catch (Exception ex)
			{
				return null;
			}

		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class ReplyVoiceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				if (value == null) return null;
				var reply = (DabGraphQlReply)value;
				return $"Requests: {reply.userTopics}";
			}
			catch (Exception ex)
			{
				return null;
			}

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
            try
            {
				if (value == null) return null;
				var topic = (DabGraphQlTopic)value;
				return $"Topics: {topic.userTopics}";
			}
            catch (Exception ex)
            {
				return null;
            }
			
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
			var res = (DabGraphQlReply)value;
            var dateTime = DateTimeOffset.Parse($"{res.createdAt} +0:00").UtcDateTime.ToLocalTime();
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
