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
using System.IO;
using Math = System.Math;
using Android.Media;

using DABApp.Droid.DependencyServices;
using Android.Telephony;

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

        DabPlayer dabplayer;

        public DroidDabNativePlayer()
        {
            player = new Android.Media.MediaPlayer() { };
            player.Completion += OnPlaybackEnded;
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
            dabplayer = Player;
            var mSession = new MediaSessionCompat(Application.Context, "MusicService");
            mSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
            var controller = mSession.Controller;
            var description = GlobalResources.playerPodcast;

            if (IntegrateWithLockScreen)
            {
                /* SET UP LOCK SCREEN */
                CreateNotificationChannel();

                dabplayer.EpisodeDataChanged += (sender, e) =>
                {
                    // Set up an intent so that tapping the notifications returns to this app:
                    Intent intent = new Intent(Application.Context, typeof(MainActivity));
                    Intent playPauseIntent = new Intent(Application.Context, typeof(SecondActivity));
                    // Create a PendingIntent; 
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
                                  .AddAction(Resource.Drawable.ic_media_play_pause, "Play", pendingIntent)
                                  .SetContentText(GlobalResources.playerPodcast.EpisodeTitle)
                                  .SetContentTitle(GlobalResources.playerPodcast.ChannelTitle);

                    // Finally, publish the notification:
                    var notificationManager = NotificationManagerCompat.From(Application.Context);
                    notificationManager.Notify(NOTIFICATION_ID, builder.Build());
                };

                dabplayer.EpisodeProgressChanged += (object sender, EventArgs e) =>
                {

                };
            }
        }


        /// Raised when audio playback completes successfully 
        public event EventHandler PlaybackEnded;

        Android.Media.MediaPlayer player;

        static int index = 0;

        /// Length of audio in seconds
        public double Duration
        {
            get
            {
                return player == null ? 0 : ((double)player.Duration) / 1000.0;
            }
        }

        /// Current position of audio playback in seconds
        public double CurrentPosition
        { get
            {
                return player == null ? 0 : ((double)player.CurrentPosition) / 1000.0;
            }
        }


        /// Playback volume (0 to 1)
        public double Volume
        {
            get {
                return _volume;
            }
            set {
                SetVolume(_volume = value);
            }
        }
        double _volume = 0.5;

        /// Indicates if the currently loaded audio file is playing
        public bool IsPlaying
        { get { return player == null ? false : player.IsPlaying; } }

        /// Indicates if the position of the loaded audio file can be updated
        public bool CanSeek
        {
            get {
                return player == null ? false : true;
            }
        }

        string path;


        /// Load wav or mp3 audio file from the iOS Resources folder
        public bool Load(string path)
        {
            player.Reset();


            if (path.ToLower().StartsWith("http", StringComparison.CurrentCulture))
            {
                //Internet resource
                player.SetAudioStreamType(Android.Media.Stream.Music);
                player.SetDataSource(path);
            }
            else
            {
                //Local file
                player.SetDataSource(path);
            }

            return PreparePlayer();
        }

        bool PreparePlayer()
        {
            player?.Prepare();

            return (player == null) ? false : true;
        }

        void DeletePlayer()
        {
            Pause();
            //Replaced stop with pause to help 2 players work side by side.
            //Stop();

            if (player != null)
            {
                player.Completion -= OnPlaybackEnded;
                player.Release();
                player.Dispose();
                player = null;
            }

            DeleteFile(path);
            path = string.Empty;
        }

        void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) == false)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        /// Begin playback or resume if paused
        public void Play()
        {
            if (player == null)
                return;

            if (IsPlaying)
            {
                //Go back to the beginning (don't start playing)... not sure what this is here for if if it ever gets hit.
                Pause();
                Seek(0);
            }
            else if (player.CurrentPosition >= player.Duration)
            {
                //Start over from the beginning if at the end of the file
                player.Pause();
                Seek(0);
            }
            else
            {
                //Play from where we're at
         
            }


            player.Start();
        }

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        public void Stop()
        {
            if (!IsPlaying)
                return;

            Pause();
            Seek(0);
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            player?.Pause();
        }

        ///<Summary>
        /// Set the current playback position (in seconds)
        ///</Summary>
        public void Seek(double position)
        {
              if (CanSeek)
            { 
                player?.SeekTo((int)position * 1000);
                System.Diagnostics.Debug.WriteLine($"Seeking to {position}");
            }
        }

        ///<Summary>
        /// Sets the playback volume as a double between 0 and 1
        /// Sets both left and right channels
        ///</Summary>
        void SetVolume(double volume)
        {
            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);

            player?.SetVolume((float)volume, (float)volume);
        }

        void OnPlaybackEnded(object sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(sender, e);

            //this improves stability on older devices but has minor performance impact
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {
                player.SeekTo(0);
                player.Stop();
                player.Prepare();
            }
        }

        bool isDisposed = false;

        ///<Summary>
		/// Dispose SimpleAudioPlayer and release resources
		///</Summary>
       	protected virtual void Dispose(bool disposing)
        {
            if (isDisposed || player == null)
                return;

            if (disposing)
                DeletePlayer();

            isDisposed = true;
        }

        //~SimpleAudioPlayerImplementation()
        //{
        //    Dispose(false);
        //}

        ///<Summary>
        /// Dispose SimpleAudioPlayer and release resources
        ///</Summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
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
