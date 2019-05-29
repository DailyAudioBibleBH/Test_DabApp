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
                    DabPlayer p = (DabPlayer)sender;
                    nowPlayingInfo.Title = $"{p.EpisodeTitle} - {p.ChannelTitle}";
                    MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfo;
                };
            }
        }
    }
}
