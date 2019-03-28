using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DABApp;
using DABApp.iOS;
using Firebase.Analytics;
using Foundation;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(AnalyticsService))]
namespace DABApp.iOS
{
    public class AnalyticsService: IAnalyticsService
    {
        public void LogEvent(string eventId)
        {
            LogEvent(eventId, (IDictionary<string, string>)null);
        }

        public void LogEvent(string eventId, string paramName, string value)
        {
            LogEvent(eventId, new Dictionary<string, string>
            {
                { paramName, value }
            });
        }

        public void LogEvent(string eventId, IDictionary<string, string> parameters)
        {

            if (parameters == null)
            {
                Analytics.LogEvent(eventId, (Dictionary<object,object>)null);
                return;
            }

            var keys = new List<NSString>();
            var values = new List<NSString>();
            foreach (var item in parameters)
            {
                keys.Add(new NSString(item.Key));
                values.Add(new NSString(item.Value));
            }

            var parametersDictionary =
                NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values.ToArray(), keys.ToArray(), keys.Count);
            Analytics.LogEvent(eventId, parametersDictionary);

        }
    }
}