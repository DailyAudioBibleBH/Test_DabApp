using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DABApp;
using DABApp.iOS;
using Firebase.Analytics;
using Foundation;
using UIKit;
using Version.Plugin;
using Xamarin.Forms;

[assembly: Dependency(typeof(AnalyticsService))]
namespace DABApp.iOS
{
    public class AnalyticsService: IAnalyticsService
    {
        public AnalyticsService()
        {
            //Check if user recently updated app and ask permissions if necessary
            string savedVersion = dbSettings.GetSetting("AppVersion", "");
            var status = AppTrackingTransparency.ATTrackingManager.TrackingAuthorizationStatus;

            //check to make sure this is not first launch so same request does not appear twice
            if (savedVersion != CrossVersion.Current.Version && savedVersion != "" && status != AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Authorized)
            {
                RequestPermission();
            }
        }

        public async void RequestPermission()
        {
            await App.Current.MainPage.DisplayAlert("App Tracking Settings", "The next prompt you will receive will ask permission to share analytical information with DAB.  We ask that you say yes.  This isn’t about targeting you. We don’t do that sort of thing. There are a ton of different devices out there.  When an app crashes we’d like to understand why so that we can keep it from happening.", "Okay");

            bool answer = await App.Current.MainPage.DisplayAlert("Update", "DAB would like to access Firebase Google Analytics for more accurate error recording.", "Accept", "Decline");
            if (answer)
            {
                Firebase.Analytics.Analytics.SetUserProperty("true", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(true);
            }

            //Store version number so not to ask again until next update
            dbSettings.StoreSetting("AppVersion", CrossVersion.Current.Version);
        }


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
            try
            {

            Debug.WriteLine($"Logging Event: {eventId}...");

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
                Debug.WriteLine($"   Logging Parameter: {item.Key} = {item.Value}");
            }

            var parametersDictionary =
                NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values.ToArray(), keys.ToArray(), keys.Count);
            Analytics.LogEvent(eventId, parametersDictionary);


            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error logging event to firebase.");
            }

        }
    }
}