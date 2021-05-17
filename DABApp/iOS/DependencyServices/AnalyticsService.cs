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

            //check if version numbers do not match -- user just updated app & if app tracking permission is denied
            //check to make sure this is not first launch so same request does not appear twice
            if (savedVersion != CrossVersion.Current.Version && savedVersion != "" && status != AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Authorized)
            {
                RequestPermission();
            }
        }

        public async void RequestPermission()
        {
            bool answer = await App.Current.MainPage.DisplayAlert("Update", "DAB would like to access analytics for more accurate error recording.", "Accept", "Decline");
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