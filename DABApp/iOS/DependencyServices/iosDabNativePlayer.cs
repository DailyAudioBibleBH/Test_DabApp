using System;
using AVFoundation;
using DABApp.DabAudio;
using DABApp.iOS;
using MediaPlayer;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(iosDabNativePlayer))]
namespace DABApp.iOS
{
    public class iosDabNativePlayer : IDabNativePlayer
    {
        DabPlayer player;
        MPNowPlayingInfo nowPlayingInfo;


        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {

            player = Player;

            /* SET UP LOCK SCREEN COMPONENTS (if needed) */

            if (IntegrateWithLockScreen)
            {
                //Remote Control (Lock Screen)
                UIApplication.SharedApplication.BeginReceivingRemoteControlEvents();
                AVAudioSession.SharedInstance().SetActive(true);
                AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord);

                nowPlayingInfo = new MPNowPlayingInfo();
                nowPlayingInfo.Artist = "Daily Audio Bible";

                player.EpisodeDataChanged += (sender, e) => 
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

                player.EpisodeProgressChanged += (object sender, EventArgs e) =>
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


    }
}
