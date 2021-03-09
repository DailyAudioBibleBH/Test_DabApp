using System;
using Android.Content;
using DABApp.DabAudio;
using DABApp.Droid;
using Android.Support.V4.App;
using Android.OS;
using Android.App;
using Application = Android.App.Application;
using Android.Media.Session;
using Android.Support.V4.Media.Session;
using System.IO;
using Math = System.Math;
using Android.Media;
using Android.Graphics;
using Android.Support.V4.Media;
using Xamarin.Forms;
using Android.Support.V4.Content;
using AndroidX.Media.Session;

[assembly: Dependency(typeof(DroidDabNativePlayer))]
namespace DABApp.Droid
{
    public class DroidDabNativePlayer : Android.App.Service, AudioManager.IOnAudioFocusChangeListener, IDabNativePlayer
    {

        //Based on Xamarin.Android documentation:
        //Fundamentals: https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/notifications/local-notifications
        //Walkthrough: https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/notifications/local-notifications-walkthrough

        static readonly int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "location_notification";
        DabPlayer dabplayer;
        bool wasPlaying = false;
        MediaSessionCompat mSession = new MediaSessionCompat(Application.Context, "MusicService");

        public PlaybackStateCode State { get; set; }


        public DroidDabNativePlayer()
        {
            player = new Android.Media.MediaPlayer() { };
            player.Completion += OnPlaybackEnded;
        }

        public bool RequestAudioFocus()
        {
            AudioManager audioManager = (AudioManager)Application.Context.GetSystemService(Android.Content.Context.AudioService);
            AudioFocusRequest audioFocusRequest;
            if (Build.VERSION.SdkInt > BuildVersionCodes.O)
            {
                audioFocusRequest = audioManager.RequestAudioFocus(new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(new AudioAttributes.Builder().SetLegacyStreamType(Android.Media.Stream.Music).Build()).SetOnAudioFocusChangeListener(this)
                .Build());
            }
            else
            {
                audioFocusRequest = audioManager.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
            }

            if (audioFocusRequest == AudioFocusRequest.Granted)
            {
                return true;
            }
            return false;
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    //Check to make sure it's not going to start playing an episode if they had already paused it before the interruption
                    if (wasPlaying == true)
                    {
                        Play();
                        State = PlaybackStateCode.Playing;
                        UpdatePlaybackState(null);
                    }
                    //Gain when other Music Player app releases the audio service   
                    break;
                case AudioFocus.Loss:
                    //We have lost focus stop!   
                    if (player.IsPlaying == true)
                        wasPlaying = true;
                    else
                        wasPlaying = false;
                    Stop();
                    State = PlaybackStateCode.Paused;
                    UpdatePlaybackState(null);
                    break;
                case AudioFocus.LossTransient:
                    //We have lost focus for a short time, but likely to resume so pause   
                    if (player.IsPlaying == true)
                        wasPlaying = true;
                    else
                        wasPlaying = false;
                    Pause();
                    State = PlaybackStateCode.Paused;
                    UpdatePlaybackState(null);
                    break;
                case AudioFocus.LossTransientCanDuck:
                    //We have lost focus but should till play at a muted 10% volume   
                    SetVolume(.1);
                    break;
            }
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

        IBinder binder;

        public override IBinder OnBind(Intent intent)
        {
            binder = new MediaPlayerServiceBinder();
            return binder;
        }

        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {
            dabplayer = Player;
            mSession.SetMetadata(
                new MediaMetadataCompat.Builder()
                    .Build()
                );
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
                    Intent playPauseIntent = new Intent(Application.Context, typeof(PlayPauseActivity));
                    Intent skipIntent = new Intent(Application.Context, typeof(SkipActivity));
                    Intent previousIntent = new Intent(Application.Context, typeof(PreviousActivity));

                    // Create a PendingIntent; 
                    const int pendingIntentId = 0;
                    const int firstPendingIntentId = 1;
                    const int skipPendingIntentId = 2;
                    const int previousPendingIntentId = 3;

                    PendingIntent backToAppPendingIntent =
                        PendingIntent.GetActivity(Application.Context, firstPendingIntentId, intent, 0);
                    PendingIntent playPausePendingIntent =
                        PendingIntent.GetActivity(Application.Context, pendingIntentId, playPauseIntent, 0);
                    PendingIntent skipPendingIntent =
                        PendingIntent.GetActivity(Application.Context, skipPendingIntentId, skipIntent, 0);
                    PendingIntent previousPendingIntent =
                        PendingIntent.GetActivity(Application.Context, previousPendingIntentId, previousIntent, 0);

                    int color = ContextCompat.GetColor(Application.Context, Resource.Color.dab_red);
                    // Build the notification:
                    var builder = new NotificationCompat.Builder(Application.Context, CHANNEL_ID)
                                  .SetStyle(new Xamarin.Android.Support.Media.Compat.NotificationCompat.MediaStyle()
                                            .SetMediaSession(mSession.SessionToken)
                                            .SetShowCancelButton(true)
                                            .SetShowActionsInCompactView(0, 1, 2)
                                            .SetCancelButtonIntent(backToAppPendingIntent))
                                  .SetProgress(player.Duration, player.CurrentPosition, true)
                                  .SetVisibility(NotificationCompat.VisibilityPublic)
                                  .SetContentIntent(backToAppPendingIntent) // Start up this activity when the user clicks the intent.
                                  .SetDeleteIntent(MediaButtonReceiver.BuildMediaButtonPendingIntent(Application.Context, PlaybackState.ActionStop))
                                  .SetSmallIcon(Resource.Drawable.AppIcon) // This is the icon to display
                                  .SetLargeIcon(BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.app_icon))
                                  .AddAction(Resource.Drawable.baseline_replay_15_white, "Backward 15", previousPendingIntent)
                                  .AddAction(Resource.Drawable.baseline_play_arrow_white_36, "Play or Pause", playPausePendingIntent)
                                  .AddAction(Resource.Drawable.baseline_forward_30_white_36, "Forward 30", skipPendingIntent)
                                  .SetShowWhen(false)
                                  .SetPriority((int)Android.App.NotificationPriority.Max)
                                  .SetContentText(GlobalResources.playerPodcast.EpisodeTitle)
                                  .SetContentTitle(GlobalResources.playerPodcast.ChannelTitle)
                                  .SetColor(color)
                                  .SetBadgeIconType(1);

                    // Finally, publish the notification:
                    var notificationManager = NotificationManagerCompat.From(Application.Context);
                    notificationManager.Notify(NOTIFICATION_ID, builder.Build());

                    mSession.Active = true;
                    mSession.SetCallback(new MediaSessionCallback());

                    //Building the lock screen
                    MediaMetadataCompat.Builder lockBuilder = new MediaMetadataCompat.Builder()
                            .PutString(MediaMetadata.MetadataKeyAlbum, GlobalResources.playerPodcast.ChannelTitle)
                            .PutString(MediaMetadata.MetadataKeyArtist, GlobalResources.playerPodcast.EpisodeDescription)
                            .PutString(MediaMetadata.MetadataKeyAlbumArtist, GlobalResources.playerPodcast.EpisodeDescription)
                            .PutString(MediaMetadata.MetadataKeyTitle, GlobalResources.playerPodcast.EpisodeTitle);
                            

                    PlaybackStateCompat.Builder stateBuilder = new PlaybackStateCompat.Builder()
                    .SetActions(
                        PlaybackStateCompat.ActionPause |
                        PlaybackStateCompat.ActionPlay |
                        PlaybackStateCompat.ActionPlayPause |
                        PlaybackStateCompat.ActionFastForward |
                        PlaybackStateCompat.ActionRewind |
                        PlaybackStateCompat.ActionStop
                    )
                    .SetState(PlaybackStateCompat.StatePlaying, player.CurrentPosition, 1.0f, SystemClock.ElapsedRealtime());

                    mSession.SetPlaybackState(stateBuilder.Build());

                    lockBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.app_icon));

                    mSession.SetMetadata(lockBuilder.Build());


                };

                dabplayer.EpisodeProgressChanged += (object sender, EventArgs e) =>
                {

                };
            }
        }


        /// Raised when audio playback completes successfully 
        public event EventHandler PlaybackEnded;

        Android.Media.MediaPlayer player;

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
        {
            get
            {
                return player == null ? 0 : ((double)player.CurrentPosition) / 1000.0;
            }
        }


        /// Playback volume (0 to 1)
        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
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
            get
            {
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
                State = PlaybackStateCode.Paused;
            }
            else if (player.CurrentPosition >= player.Duration)
            {
                //Start over from the beginning if at the end of the file
                player.Pause();
                Seek(0);
                State = PlaybackStateCode.Paused;
            }
            else
            {
                //Play from where we're at
                State = PlaybackStateCode.Playing;
            }

            if (RequestAudioFocus())
            {
                player.Start();
                State = PlaybackStateCode.Playing;
            }
            UpdatePlaybackState(null);
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
            State = PlaybackStateCode.Stopped;
            UpdatePlaybackState(null);
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            player?.Pause();
            State = PlaybackStateCode.Paused;
            UpdatePlaybackState(null);
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
                State = PlaybackStateCode.Stopped;
                UpdatePlaybackState(null);
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

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        void UpdatePlaybackState(string error)
        {
            var position = PlaybackState.PlaybackPositionUnknown;
            if (player != null)
            {
                position = player.CurrentPosition;
            }

            PlaybackStateCompat.Builder stateBuilder = new PlaybackStateCompat.Builder()
                    .SetActions(
                        PlaybackStateCompat.ActionPause |
                        PlaybackStateCompat.ActionPlay |
                        PlaybackStateCompat.ActionPlayPause |
                        PlaybackStateCompat.ActionFastForward |
                        PlaybackStateCompat.ActionRewind |
                        PlaybackStateCompat.ActionStop
                    );

            PlaybackStateCode state = State;

            // If there is an error message, send it to the playback state:
            if (error != null)
            {
                // Error states are really only supposed to be used for errors that cause playback to
                // stop unexpectedly and persist until the user takes action to fix it.
                stateBuilder.SetErrorMessage(error);
                state = PlaybackStateCode.Error;
            }

            stateBuilder.SetState(Convert.ToInt32(state), position, 1.0f, SystemClock.ElapsedRealtime());
            
            mSession.SetPlaybackState(stateBuilder.Build());
        }
    }

    [Activity]
    public class PlayPauseActivity : Activity
    {
        DabPlayer player = GlobalResources.playerPodcast;
        EpisodeViewModel Episode;

        protected override void OnCreate(Bundle bundle)
        {
            try
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
            catch (System.Exception ex)
            {

            }

        }
    }

    [Activity]
    public class SkipActivity : Activity
    {
        DabPlayer player = GlobalResources.playerPodcast;
        EpisodeViewModel Episode;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            bool playing = player.IsPlaying;

            if (player.IsReady)
            {
                MessagingCenter.Send<string>("droid", "skip");
            }
            else
            {
                if (player.Load(Episode.Episode))
                {
                    MessagingCenter.Send<string>("droid", "skip");
                }
                else
                {
                    //DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                }

            }

            Finish();
        }
    }

    [Activity]
    public class PreviousActivity : Activity
    {
        DabPlayer player = GlobalResources.playerPodcast;
        EpisodeViewModel Episode;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            bool playing = player.IsPlaying;

            if (player.IsReady)
            {
                player.Seek(player.CurrentPosition - 15);
            }
            else
            {
                if (player.Load(Episode.Episode))
                {
                    player.Seek(player.CurrentPosition - 15);

                }
                else
                {
                    //DisplayAlert("Episode Unavailable", "The episode you are attempting to play is currently unavailable. Please try again later.", "OK");
                }

            }

            Finish();
        }
    }

    public class MediaSessionCallback : MediaSessionCompat.Callback
    {
        DabPlayer player = GlobalResources.playerPodcast;
        private MediaPlayerServiceBinder mediaPlayerService = new MediaPlayerServiceBinder();
        public MediaSessionCallback()
        {
        }

        public override void OnPause()
        {
            mediaPlayerService.GetMediaPlayerService().Pause();
            base.OnPause();
        }

        public override void OnPlay()
        {
            mediaPlayerService.GetMediaPlayerService().Play();
            base.OnPlay();
        }

        public override void OnFastForward()
        {
            mediaPlayerService.GetMediaPlayerService().Seek(player.CurrentPosition + 30);
            base.OnFastForward();
        }

        public override void OnRewind()
        {
            mediaPlayerService.GetMediaPlayerService().Seek(player.CurrentPosition - 15);
            base.OnRewind();
        }

        public override void OnStop()
        {
            mediaPlayerService.GetMediaPlayerService().Stop();
            base.OnStop();
        }
    }

    public class MediaPlayerServiceBinder : Binder
    {
        private DabPlayer service;

        public MediaPlayerServiceBinder()
        {
            this.service = GlobalResources.playerPodcast;
        }

        public DabPlayer GetMediaPlayerService()
        {
            return service;
        }
    }
}