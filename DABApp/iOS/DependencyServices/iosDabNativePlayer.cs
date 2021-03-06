using System;
using System.Diagnostics;
using System.IO;
using AVFoundation;
using DABApp.DabAudio;
using DABApp.iOS;
using Foundation;
using MediaPlayer;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(iosDabNativePlayer))]
namespace DABApp.iOS
{
    public class iosDabNativePlayer : IDabNativePlayer
    {
        DabPlayer dabplayer;
        MPNowPlayingInfo nowPlayingInfo;


        public iosDabNativePlayer()
        {
            Debug.WriteLine("Creating iosDabNativePlayer");
        }

        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {

            dabplayer = Player;
            AVAudioSession.SharedInstance().SetActive(true);
            AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);


            /* SET UP LOCK SCREEN COMPONENTS (if needed) */

            if (IntegrateWithLockScreen)
            {
                //Remote Control (Lock Screen)
                UIApplication.SharedApplication.BeginReceivingRemoteControlEvents();

                nowPlayingInfo = new MPNowPlayingInfo();
                nowPlayingInfo.Artist = "Daily Audio Bible";

                dabplayer.EpisodeDataChanged += (sender, e) =>
                {
                    //Update the lock screen with new playing info
                    DabPlayerEventArgs args = (DabPlayerEventArgs)e;
                    nowPlayingInfo.Title = args.EpisodeTitle;
                    nowPlayingInfo.AlbumTitle = args.ChannelTitle;
                    nowPlayingInfo.Artist = "Daily Audio Bible";
                    nowPlayingInfo.PlaybackDuration = args.Duration;
                    nowPlayingInfo.ElapsedPlaybackTime = args.CurrentPosition;
                    MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfo;
                };

                dabplayer.EpisodeProgressChanged += (object sender, EventArgs e) =>
                {
                    //Update lock screen with nee position info
                    DabPlayerEventArgs args = (DabPlayerEventArgs)e;
                    nowPlayingInfo.Title = args.EpisodeTitle;
                    nowPlayingInfo.AlbumTitle = args.ChannelTitle;
                    nowPlayingInfo.Artist = "Daily Audio Bible";
                    nowPlayingInfo.PlaybackDuration = args.Duration;
                    nowPlayingInfo.ElapsedPlaybackTime = args.CurrentPosition;
                    MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfo;
                };

                /* Lock Screen Events */

                //Handle play event from lock screen
                MPRemoteCommandCenter.Shared.PlayCommand.AddTarget((arg) =>
                {
                    try
                    {
                        dabplayer.Play();
                        return MPRemoteCommandHandlerStatus.Success;
                    }
                    catch (Exception ex)
                    {
                        return MPRemoteCommandHandlerStatus.CommandFailed;
                    }
                });

                //Handle pause event from lock scren
                MPRemoteCommandCenter.Shared.PauseCommand.AddTarget((arg) =>
                {
                    try
                    {
                        dabplayer.Pause();
                        return MPRemoteCommandHandlerStatus.Success;
                    }
                    catch (Exception ex)
                    {
                        return MPRemoteCommandHandlerStatus.CommandFailed;
                    }

                });


                //Handle skip forward command from lock screen
                MPRemoteCommandCenter.Shared.SkipForwardCommand.AddTarget((arg) =>
                {
                    try
                    {
                        dabplayer.Skip(10); //icon says 15 seconds
                        return MPRemoteCommandHandlerStatus.Success;
                    }
                    catch (Exception ex)
                    {
                        return MPRemoteCommandHandlerStatus.CommandFailed;
                    }

                });

                //Handle skip backward command from lock screen
                MPRemoteCommandCenter.Shared.SkipBackwardCommand.AddTarget((arg) =>
                {
                    try
                    {
                        dabplayer.Skip(-10); //icon says 15 seconds
                        return MPRemoteCommandHandlerStatus.Success;
                    }
                    catch (Exception ex)
                    {
                        return MPRemoteCommandHandlerStatus.CommandFailed;
                    }

                });

            }
        }



        /// Raised when playback completes
        public event EventHandler PlaybackEnded;

        AVPlayer player;

        /// Length of audio in seconds
        public double Duration
        {
            get
            {
                return player == null ? 0 : player.CurrentItem.Asset.Duration.Seconds;
            }
        }

        /// Current position of audio in seconds
        public double CurrentPosition
        {
            get
            {
                try
                {

                    return player == null ? 0 : player.CurrentItem.CurrentTime.Seconds;
                }
                catch (Exception ex)
                {
                    return 0;
                }

            }
        }

        /// Playback volume (0 to 1)
        public double Volume
        {
            get { return player == null ? 0 : player.Volume; }
            set { SetVolume(value); }
        }

        /// Indicates if the currently loaded audio file is playing
        public bool IsPlaying
        {
            get
            {
                return player == null ? false : (Math.Abs(player.Rate) > double.Epsilon);
            }
        }

        /// Indicates if the position of the loaded audio file can be updated - always returns true on iOS
        public bool CanSeek
        {
            get
            {
                return player == null ? false : true;
            }
        }

        /// Load audio file from local or web-based resource
        public bool Load(string path)
        {
            DeletePlayer();

            if (path.ToLower().StartsWith("http", StringComparison.CurrentCulture))
            {
                //Internet resource
                NSUrl u = NSUrl.FromString(path);
                player = AVPlayer.FromUrl(u);

            }
            else
            {
                //Local file
                if (!path.ToLower().StartsWith("file", StringComparison.CurrentCulture))
                {
                    path = path.Insert(0, "file://");
                }
                NSUrl u = NSUrl.FromString(path);
                player = AVPlayer.FromUrl(u);
            }

            return PreparePlayer();


        }

        private NSObject _OnPlaybackEndedHandle;

        bool PreparePlayer()
        {
            if (player != null)
            {
                //Set up the player categories
                AVAudioSessionCategoryOptions audioOptions = AVAudioSessionCategoryOptions.AllowAirPlay |
    AVAudioSessionCategoryOptions.AllowBluetooth |
    AVAudioSessionCategoryOptions.AllowBluetoothA2DP |
    AVAudioSessionCategoryOptions.DefaultToSpeaker |
    AVAudioSessionCategoryOptions.InterruptSpokenAudioAndMixWithOthers;
                AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback, audioOptions);


                //Register for a notification that playback ended
                _OnPlaybackEndedHandle = NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, OnPlaybackEnded);

                //Register for a notification that playback was interupted
                AVAudioSession.Notifications.ObserveInterruption(OnPlaybackInterupted);


                //Prepare to play the file by play/pausing it
                player.Play();
                player.Pause();
            }

            return (player == null) ? false : true;
        }

        private void OnPlaybackInterupted(object sender, AVAudioSessionInterruptionEventArgs e)
        {
            //Handle player interupttions by a call. Only fires if the player is currently playing
            switch (e.InterruptionType)
            {
                case AVAudioSessionInterruptionType.Began:
                    if (dabplayer.IsReady)
                    {
                        if (dabplayer.IsPlaying)
                        {
                            dabplayer.PauseForCall();
                        }
                    }
                    break;
                case AVAudioSessionInterruptionType.Ended:
                    if (dabplayer.IsReady && !dabplayer.IsPlaying && dabplayer.ShouldResumePlay())
                    {
                        dabplayer.ResumePlay();
                    }
                    break;

            }

        }

        void DeletePlayer()
        {
            Pause();

            if (player != null)
            {
                //Unregister the notification
                NSNotificationCenter.DefaultCenter.RemoveObserver(_OnPlaybackEndedHandle);

                //dispose of the player
                player.Dispose();
                player = null;
            }
        }



        //Playback has ended - raise event
        private void OnPlaybackEnded(NSNotification notification)
        {
            PlaybackEnded?.Invoke(this, null);
        }

        /// Begin playback or resume if paused
        public void Play()
        {
            try
            {

                if (player == null)
                    return;


                if (IsPlaying)
                {
                    //Go back to the beginning (don't start playing)... not sure what this is here for if if it ever gets hit.
                    player.Seek(new CoreMedia.CMTime(0, 1));
                }
                else if (player.CurrentTime >= player.CurrentItem.Duration)
                {
                    //Start over from the beginning if at the end of the file
                    player.Seek(new CoreMedia.CMTime(0, 1));
                    player.Play();
                }
                else
                {
                    //Play from where we're at
                    player?.Play();
                }
            }
            catch (Exception ex)
            {

            }

        }

        /// Pause playback if playing (does not resume)
        public void Pause()
        {
            player?.Pause();
        }

        /// Stop playack and set the current position to the beginning
        public void Stop()
        {
            player?.Pause();
            Seek(0);
        }

        /// Seek a position in seconds in the currently loaded sound file 
        public void Seek(double position)
        {
            if (player != null)
            {
                int seconds = (int)position;
                player.Seek(new CoreMedia.CMTime(seconds, 1));
            }
        }

        //Set volume of player (0 - 1)
        void SetVolume(double volume)
        {
            if (player == null)
                return;

            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);
            player.Volume = (float)volume;
        }

        //Fires Playback Ended event
        void OnPlaybackEnded()
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }

        bool isDisposed = false;
        /// Dispose SimpleAudioPlayer and release resources
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
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


    }
}
