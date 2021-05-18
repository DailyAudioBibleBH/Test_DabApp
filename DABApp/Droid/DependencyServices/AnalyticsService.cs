﻿using System;
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
using Plugin.CurrentActivity;
using Version.Plugin;
using Xamarin.Forms;

[assembly: Dependency(typeof(AnalyticsService))]
namespace DABApp.Droid
{
    public class AnalyticsService : IAnalyticsService
    {
        public AnalyticsService()
        {
            //Check version number and update if needed
            string savedVersion = dbSettings.GetSetting("AppVersion", "");

            //First launch is updated in main activity, other android updates update version number here to match ios structure
            if (savedVersion != CrossVersion.Current.Version && savedVersion != "")
            {
                dbSettings.StoreSetting("AppVersion", CrossVersion.Current.Version);
            }
        }

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
            try
            {



                var fireBaseAnalytics = FirebaseAnalytics.GetInstance(CrossCurrentActivity.Current.AppContext);

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error logging event to firebase");
            }
        }
    }
}