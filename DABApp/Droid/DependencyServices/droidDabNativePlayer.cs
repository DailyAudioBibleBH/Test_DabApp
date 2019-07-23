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
using Java.Lang;
using Android.Content.Res;
using Android.Media.Session;
using Android.Support.V4.Media.Session;
using Plugin.SimpleAudioPlayer;

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

        DabPlayer player;

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
            var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Low)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)Application.Context.GetSystemService(Android.Content.Context.NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {
            player = Player;
            var mSession = new MediaSessionCompat(Application.Context, "MusicService");
            mSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
            var controller = mSession.Controller;
            var description = GlobalResources.playerPodcast;

            if (IntegrateWithLockScreen)
            {
                /* SET UP LOCK SCREEN */
                CreateNotificationChannel();

                player.EpisodeDataChanged += (sender, e) =>
                {
                    // Set up an intent so that tapping the notifications returns to this app:
                    Intent intent = new Intent(Application.Context, typeof(MainActivity));
                    Intent playPauseIntent = new Intent(Application.Context, typeof(SecondActivity));
                    // Create a PendingIntent; we're only using one PendingIntent (ID = 0):
                    const int pendingIntentId = 0;
                    const int firstPendingIntentId = 1;
                    PendingIntent firstPendingIntent =
                        PendingIntent.GetActivity(Application.Context, firstPendingIntentId, intent, 0);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(Application.Context, pendingIntentId, playPauseIntent, 0);

                    // Build the notification:
                    var builder = new NotificationCompat.Builder(Application.Context, CHANNEL_ID)
                                  .SetStyle(new Android.Support.V4.Media.App.NotificationCompat.MediaStyle()
                                            .SetMediaSession(mSession.SessionToken)
                                            .SetShowActionsInCompactView(0))
                                  .SetVisibility(NotificationCompat.VisibilityPublic)
                                  .SetContentIntent(firstPendingIntent) // Start up this activity when the user clicks the intent.
                                  .SetDeleteIntent(MediaButtonReceiver.BuildMediaButtonPendingIntent(Application.Context, PlaybackState.ActionStop))
                                  .SetSmallIcon(Resource.Drawable.app_icon) // This is the icon to display
                                  .AddAction(Resource.Drawable.ic_media_play_dark, "Play", pendingIntent)
                                  .SetContentText(GlobalResources.playerPodcast.EpisodeTitle)
                                  .SetContentTitle(GlobalResources.playerPodcast.ChannelTitle);

                    // Finally, publish the notification:
                    var notificationManager = NotificationManagerCompat.From(Application.Context);
                    notificationManager.Notify(NOTIFICATION_ID, builder.Build());                   
                };

                player.EpisodeProgressChanged += (object sender, EventArgs e) =>
                {

                };
            }
        }
    }

    [Activity]
    public class SecondActivity : Activity
    {
        DabPlayer player = GlobalResources.playerPodcast;
        EpisodeViewModel Episode;
        DroidDabNativePlayer droid = new DroidDabNativePlayer();
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (player.IsReady)
            {
                if (player.IsPlaying)
                {
                    player.Pause();
                }
                else
                {
                    player.Play();
                }
            }
            else
            {
                if (player.Load(Episode.Episode))
                {
                    player.Play();
                }
                else
                {
                    //DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                }

            }

            Finish();
        }
    }
}
