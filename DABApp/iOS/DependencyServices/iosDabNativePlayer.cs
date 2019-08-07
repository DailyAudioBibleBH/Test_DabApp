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
        /// Raised when playback completes or loops
        ///</Summary>
        public event EventHandler PlaybackEnded;

        AVPlayer player;

        ///<Summary>
        /// Length of audio in seconds
        ///</Summary>
        public double Duration
        { get { return player == null ? 0 : player.CurrentItem.Asset.Duration.Seconds; } }

        ///<Summary>
        /// Current position of audio in seconds
        ///</Summary>
        public double CurrentPosition
        { get { return player == null ? 0 : player.CurrentTime.Seconds; } }

        ///<Summary>
        /// Playback volume (0 to 1)
        ///</Summary>
        public double Volume
        {
            get { return player == null ? 0 : player.Volume; }
            set { SetVolume(value, Balance); }
        }

        ///<Summary>
        /// Balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right
        ///</Summary>
        public double Balance
        {
            get { return _balance; }
            set { SetVolume(Volume, _balance = value); }
        }
        double _balance = 0;

        ///<Summary>
        /// Indicates if the currently loaded audio file is playing
        ///</Summary>
        public bool IsPlaying
        { get { return player == null ? false : (player.Rate != 0); } }

        ///<Summary>
        /// Continously repeats the currently playing sound
        ///</Summary>
        public bool Loop
        {
            get { return _loop; }
            set
            {
                _loop = value;
                //if (player != null)
                //    player.NumberOfLoops = _loop ? -1 : 0;
            }
        }
        bool _loop;

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated - always returns true on iOS
        ///</Summary>
        public bool CanSeek
        { get { return player == null ? false : true; } }



        ///<Summary>
        /// Load wave or mp3 audio file as a stream
        ///</Summary>
        public bool Load(Stream audioStream)
        {
            DeletePlayer();

            var data = NSData.FromStream(audioStream);
            throw new NotImplementedException();
//            player = AVAudioPlayer.FromData(data);

            return PreparePlayer();
        }

        ///<Summary>
        /// Load wave or mp3 audio file from the Android assets folder
        ///</Summary>
        public bool Load(string fileName)
        {
            DeletePlayer();
            throw  new NotImplementedException();
//            player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(fileName));

            return PreparePlayer();
        }

        bool PreparePlayer()
        {
            if (player != null)
            {
               // player.FinishedPlaying += OnPlaybackEnded;
              //  player.PrepareToPlay();
            }

            return (player == null) ? false : true;
        }

        void DeletePlayer()
        {
            Stop();

            if (player != null)
            {
               // player.FinishedPlaying -= OnPlaybackEnded;
                player.Dispose();
                player = null;
            }
        }

        private void OnPlaybackEnded(object sender, AVStatusEventArgs e)
        {
            PlaybackEnded?.Invoke(sender, e);
        }

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        public void Play()
        {
            if (player == null)
                return;

            if (player.Rate != 0)
                player.Seek(new CoreMedia.CMTime(0,0)); // CurrentTime = 0;
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
                return;
            //player.Seek(new CoreMedia.CMTime(long.Parse(position.ToString()), 0));
        }

        void SetVolume(double volume, double balance)
        {
            if (player == null)
                return;

            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);

            balance = Math.Max(-1, balance);
            balance = Math.Min(1, balance);

            player.Volume = (float)volume;
            //player.Pan = (float)balance;
        }
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

        //SimpleAudioPlayerImplementation()
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

        public bool LoadUrl(string url)
        {
            DeletePlayer();

            NSUrl u = NSUrl.FromString(url);
            player = AVPlayer.FromUrl(u);

            return PreparePlayer();
        }
    }
}
