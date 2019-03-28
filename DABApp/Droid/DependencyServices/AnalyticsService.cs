using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.Droid;
using Firebase.Analytics;
using Xamarin.Forms;

[assembly: Dependency(typeof(AnalyticsService))]
namespace DABApp.Droid
{ 
    public class AnalyticsService: IAnalyticsService
    {
        public void LogEvent(string eventId)
        {
            LogEvent(eventId, null);
        }

        public void LogEvent(string eventId, string paramName, string value)
        {
            LogEvent(eventId, new Dictionary<string, string>
            {
                {paramName, value}
            });
        }

        public void LogEvent(string eventId, IDictionary<string, string> parameters)
        {
            var fireBaseAnalytics = FirebaseAnalytics.GetInstance(Forms.Context);

            if (parameters == null)
            {
                fireBaseAnalytics.LogEvent(eventId, null);
                return;
            }

            var bundle = new Bundle();

            foreach (var item in parameters)
            {
                bundle.PutString(FirebaseAnalytics.Param.ItemId, item.Key);
                bundle.PutString(FirebaseAnalytics.Param.ItemName, item.Value);
            }

            fireBaseAnalytics.LogEvent(eventId, bundle);
        }
    }
}