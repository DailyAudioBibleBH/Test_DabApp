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
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(AnalyticsService))]
namespace DABApp.iOS
{
    public class AnalyticsService: IAnalyticsService
    {
        public async void FirstLaunchPromptUserForPermissions()
        {
            //if version 14 or higher
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current.MainPage.DisplayAlert("App Tracking Settings", "The next prompt you will receive will ask permission to share analytical information with DAB.  We ask that you say Allow Tracking.  This isn’t about targeting you. We don’t do that sort of thing. There are a ton of different devices out there.  When an app crashes we’d like to understand why so that we can keep it from happening.", "Okay");

                    //Request Permission to follow AppTrackingTransparency guidelines
                    AppTrackingTransparency.ATTrackingManager.RequestTrackingAuthorization((result) =>
                    {
                        switch (result)
                        {
                            case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.NotDetermined:
                                Firebase.Analytics.Analytics.SetUserProperty("false", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                                Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(false);
                                break;
                            case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Restricted:
                                Firebase.Analytics.Analytics.SetUserProperty("false", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                                Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(false);
                                break;
                            case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Denied:
                                Firebase.Analytics.Analytics.SetUserProperty("false", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                                Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(false);
                                break;
                            case AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Authorized:
                                Firebase.Analytics.Analytics.SetUserProperty("true", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                                Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(true);
                                break;
                            default:
                                break;
                        }
                    });
                });
            }
        }

        public async void RequestPermission()
        {
            var status = AppTrackingTransparency.ATTrackingManager.TrackingAuthorizationStatus;
            if (status != AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Authorized)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current.MainPage.DisplayAlert("App Tracking Settings", "The next prompt you will receive will ask permission to share analytical information with DAB.  We ask that you say Allow Tracking.  This isn’t about targeting you. We don’t do that sort of thing. There are a ton of different devices out there.  When an app crashes we’d like to understand why so that we can keep it from happening.", "Okay");

                    bool answer = await App.Current.MainPage.DisplayAlert("App Tracking Settings", "DAB would like to access Firebase Google Analytics for more accurate error recording.", "Allow Tracking", "Ask App Not To Track");
                    if (answer)
                    {
                        Firebase.Analytics.Analytics.SetUserProperty("true", Firebase.Analytics.UserPropertyNamesConstants.AllowAdPersonalizationSignals);
                        Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(true);
                    }
                });
            }
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