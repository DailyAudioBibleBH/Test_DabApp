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
			DateTime beforeDate = topic.lastActive.ToLocalTime();
			DateTime futureDate = DateTime.Now.ToLocalTime();

			int minutes = 0;
			int hours = 0;
			int days = 0;
			int weeks = 0;
			int months = 0;
			int years = 0;

			Dictionary<int, int> dictMonths = new Dictionary<int, int> { };
			dictMonths.Add(1, 31);
			if (DateTime.IsLeapYear(futureDate.Year))
				dictMonths.Add(2, 29);
			else
				dictMonths.Add(2, 28);
			dictMonths.Add(3, 31);
			dictMonths.Add(4, 30);
			dictMonths.Add(5, 31);
			dictMonths.Add(6, 30);
			dictMonths.Add(7, 31);
			dictMonths.Add(8, 31);
			dictMonths.Add(9, 30);
			dictMonths.Add(10, 31);
			dictMonths.Add(11, 30);
			dictMonths.Add(12, 31);

			//Time difference between dates
			TimeSpan span = futureDate - beforeDate;
			hours = span.Hours;
			minutes = span.Minutes;

			//Days total
			days = span.Days;
			//Find how many years
			DateTime zeroTime = new DateTime(1, 1, 1);

			// Because we start at year 1 for the Gregorian
			// calendar, we must subtract a year here.
			years = (zeroTime + span).Year - 1;
			//find difference of days of years already found
			int startYear = futureDate.Year - years;
			for (int i = 0; i < years; i++)
			{
				if (DateTime.IsLeapYear(startYear))
					days -= 366;
				else
					days -= 365;

				startYear++;
			}
			//Find months by multiplying months in year by difference of datetime years then add difference of current year months
			months = 12 * (futureDate.Year - beforeDate.Year) + (futureDate.Month - beforeDate.Month);
			//month may need to be decremented because the above calculates the ceiling of the months, not the floor.
			//to do so we increase before by the same number of months and compare.
			//(500ms fudge factor because datetimes are not precise enough to compare exactly)
			if (futureDate.CompareTo(beforeDate.AddMonths(months).AddMilliseconds(-500)) <= 0)
			{
				--months;
			}
			//subtract months from how many years we have already accumulated
			months -= (12 * years);
			//find how many days by compared to our month dictionary
			int startMonth = beforeDate.Month;
			for (int i = 0; i < months; i++)
			{
				//check if faulty leap year
				if (startMonth == 2 && (months - 1 > 10))
					days -= 28;
				else
					days -= dictMonths[startMonth];
				startMonth++;
				if (startMonth > 12)
				{
					startMonth = 1;
				}
			}
			//Find if any weeks are within our now total days
			weeks = days / 7;
			if (weeks > 0)
			{
				//remainder is days left
				days = days % 7;
			}

			Console.WriteLine(years + " " + months + " " + weeks + " " + days + " " + span.Hours + " " + span.Minutes);

            if (years > 0)
            {
				if (years > 1 && months > 1)
					return $"Latest Reply: {years} years, {months} months ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (years > 1 && months == 0)
					return $"Latest Reply: {years} years ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (years == 1 && months == 0)
					return $"Latest Reply: {years} year ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (years > 1)
					return $"Latest Reply: {years} years, {months} month ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (months > 1)
					return $"Latest Reply: {years} year, {months} months ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else
					return $"Latest Reply: {years} year, {months} month ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else if (months > 0)
            {
				if (months > 1 && weeks > 1)
					return $"Latest Reply: {months} months, {weeks} weeks ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (months > 1 && weeks == 0)
					return $"Latest Reply: {months} months ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
                else if (months == 1 && weeks == 0)
					return $"Latest Reply: {months} month ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (months > 1)
					return $"Latest Reply: {months} months, {weeks} week ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (weeks > 1)
					return $"Latest Reply: {months} month, {weeks} weeks ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else
					return $"Latest Reply: {months} month, {weeks} week ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else if (weeks > 0)
            {
                if (weeks > 1 && days > 1)
					return $"Latest Reply: {weeks} weeks, {days} days ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (weeks > 1 && days == 0)
					return $"Latest Reply: {weeks} weeks ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (weeks == 1 && days == 0)
					return $"Latest Reply: {weeks} week ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (weeks > 1)
					return $"Latest Reply: {weeks} weeks, {days} day ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
                else if (days > 1)
					return $"Latest Reply: {weeks} week, {days} days ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else
					return $"Latest Reply: {weeks} week, {days} day ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
			else if (days > 0)
            {
				if (days > 1 && hours > 1)
					return $"Latest Reply: {days} days, {hours} hours ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (days > 1 && hours == 0)
					return $"Latest Reply: {days} days ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (days == 1 && hours == 0)
					return $"Latest Reply: {days} day ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (days > 1)
					return $"Latest Reply: {days} days, {hours} hour ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (hours > 1)
					return $"Latest Reply: {days} day, {hours} hours ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else
					return $"Latest Reply: {days} day, {hours} hour ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else if (hours > 0)
            {
				if (hours > 1 && minutes > 1)
					return $"Latest Reply: {hours} hours, {minutes} minutes ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (hours > 1 && minutes == 0)
					return $"Latest Reply: {hours} hours ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (hours == 1 && minutes == 0)
					return $"Latest Reply: {hours} hour ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (hours > 1)
					return $"Latest Reply: {hours} hours, {minutes} minute ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else if (minutes > 1)
					return $"Latest Reply: {hours} hour, {minutes} minutes ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else
					return $"Latest Reply: {hours} hour, {minutes} minute ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else if (minutes > 0)
            {
				if (minutes > 1)
					return $"Latest Reply: {minutes} minutes ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
				else
					return $"Latest Reply: {minutes} minute ago.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
			}
            else
            {
				return $"Latest Reply: Just now.  Voices: {topic.voiceCount}  Prayers: {topic.replyCount}";
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
