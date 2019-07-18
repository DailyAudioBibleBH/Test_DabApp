using System;
using Android.Content;
using DABApp.DabAudio;
using DABApp.Droid;
using Xamarin.Forms;
using Android.Support.V4.App;
using TaskStackBuilder = Android.Support.V4.App.TaskStackBuilder;
using Android.OS;
using Android.App;
using Application = Android.App.Application;

[assembly: Dependency(typeof(DroidDabNativePlayer))]
namespace DABApp.Droid
{



    public class DroidDabNativePlayer : IDabNativePlayer
    {

        //Based on Xamarin.Android documentation:
        //Fundamentals: https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/notifications/local-notifications
        //Walkthrough: https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/notifications/local-notifications-walkthrough


        static readonly int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "location_notification";
        internal static readonly string COUNT_KEY = "count";

        DabPlayer player;
        int count = 0;

        public DroidDabNativePlayer()
        {
        }

        //Static method to create a single notification channel;
        static void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var name = "DAB Notifications";
            var description = "DAB Notification Descriptions";
            var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)Application.Context.GetSystemService(Android.Content.Context.NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {
            player = Player;


            if (IntegrateWithLockScreen)
            {
                /* SET UP LOCK SCREEN */
                CreateNotificationChannel();

                player.EpisodeDataChanged += (sender, e) =>
                {
                    // Pass the current button press count value to the next activity:
                    var valuesForActivity = new Bundle();
                    valuesForActivity.PutInt(COUNT_KEY, count);

                    // When the user clicks the notification, SecondActivity will start up.
                    //var resultIntent = new Intent(Application.Context, typeof(SecondActivity));

                    // Pass some values to SecondActivity:
                    //resultIntent.PutExtras(valuesForActivity);

                    // Construct a back stack for cross-task navigation:
                    //var stackBuilder = TaskStackBuilder.Create(this);
                    //stackBuilder.AddParentStack(Class.FromType(typeof(SecondActivity)));
                    //stackBuilder.AddNextIntent(resultIntent);

                    // Create the PendingIntent with the back stack:
                    //var resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

                    // Build the notification:
                    var builder = new NotificationCompat.Builder(Application.Context, CHANNEL_ID)
                                  .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                                                       //.SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                                  .SetContentTitle(GlobalResources.playerPodcast.ChannelTitle) // Set the title
                                  .SetNumber(count) // Display the count in the Content Info
                                    .SetSmallIcon(Resource.Drawable.app_icon) // This is the icon to display
                                  .SetContentText(GlobalResources.playerPodcast.EpisodeTitle); // the message to display.

                    // Finally, publish the notification:
                    var notificationManager = NotificationManagerCompat.From(Application.Context);
                    notificationManager.Notify(NOTIFICATION_ID, builder.Build());

                    // Increment the button press count:
                    count++;
                };

                player.EpisodeProgressChanged += (object sender, EventArgs e) =>
                {

                };


            }


        }


    }
}
