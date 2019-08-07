using System;
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
                        GlobalResources.playerPodcast.Play();
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
                        GlobalResources.playerPodcast.Pause();
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
                        GlobalResources.playerPodcast.Skip(15); //icon says 15 seconds
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
                        GlobalResources.playerPodcast.Skip(-15); //icon says 15 seconds
                        return MPRemoteCommandHandlerStatus.Success;
                    }
                    catch (Exception ex)
                    {
                        return MPRemoteCommandHandlerStatus.CommandFailed;
                    }

                });

            }
        }



        ///<Summary>
        /// Raised when playback completes
        ///</Summary>
        public event EventHandler PlaybackEnded;

        AVPlayer player;

        ///<Summary>
        /// Length of audio in seconds
        ///</Summary>
        public double Duration
        { get
            {
                return player == null ? 0 : player.CurrentItem.Asset.Duration.Seconds;
            }
        }

        ///<Summary>
        /// Current position of audio in seconds
        ///</Summary>
        public double CurrentPosition
        { get
            {
                return player == null ? 0 : player.CurrentItem.CurrentTime.Seconds;
            }
        }

        ///<Summary>
        /// Playback volume (0 to 1)
        ///</Summary>
        public double Volume
        {
            get { return player == null ? 0 : player.Volume; }
            set { SetVolume(value); }
        }

        ///<Summary>
        /// Indicates if the currently loaded audio file is playing
        ///</Summary>
        public bool IsPlaying
        { get
            {
                return player == null ? false : (Math.Abs(player.Rate) > double.Epsilon);
            }
        }

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated - always returns true on iOS
        ///</Summary>
        public bool CanSeek
        { get
            {
                return player == null ? false : true;
            }
        }

        ///<Summary>
        /// Load audio file from local or web-based resource
        ///</Summary>
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
                if (path.ToLower().StartsWith("file", StringComparison.CurrentCulture))
                {
                    path.Insert(0, "file://");
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
                //Register for a notification that playback ended
                _OnPlaybackEndedHandle = NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, OnPlaybackEnded);

                //Prepare to play the file by play/pausing it
                player.Play();
                player.Pause();
            }

            return (player == null) ? false : true;
        }

        void DeletePlayer()
        {
            Stop();

            if (player != null)
            {
                //Unregister the notification
                NSNotificationCenter.DefaultCenter.RemoveObserver(_OnPlaybackEndedHandle);

                //dispose of the player
                player.Dispose();
                player = null;
            }
        }

        private void OnPlaybackEnded(NSNotification notification)
        {
            PlaybackEnded?.Invoke(this, null);
        }

        //private void OnPlaybackEnded(object sender, AVStatusEventArgs e)
        //{
        //    PlaybackEnded?.Invoke(sender, e);
        //}

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        public void Play()
        {
            if (player == null)
                return;

            if (IsPlaying)
                //Go back to the beginning
                player.Seek(new CoreMedia.CMTime(0, 1));
            else
                player?.Play();
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            player?.Pause();
        }

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        public void Stop()
        {
            player?.Pause();
            Seek(0);
        }

        ///<Summary>
        /// Seek a position in seconds in the currently loaded sound file 
        ///</Summary>
        public void Seek(double position)
        {
            if (player == null)
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
        ///<Summary>
        /// Dispose SimpleAudioPlayer and release resources
        ///</Summary>
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
