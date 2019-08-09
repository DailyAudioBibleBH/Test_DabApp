using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Views;
using Android.Widget;
using DABApp.DabAudio;

namespace DABApp.Droid.DependencyServices
{
    public class CallReceiver : PhoneStateListener
    {
        TelephonyManager telManager;
        Context context;
        
        public override void OnCallStateChanged([GeneratedEnum] CallState state, string incomingNumber)
        {
            base.OnCallStateChanged(state, incomingNumber);
            DabPlayer player = GlobalResources.playerPodcast;
            try
            {
                    switch (state)
                {
                    case CallState.Ringing:
                        {
                            if (player.IsReady)
                            {
                                if (player.IsPlaying)
                                {
                                    player.PauseForCall();
                                }
                            }
                            //PAUSE
                            break;
                        }
                    case CallState.Idle:
                        {
                            if (player.IsReady && !player.IsPlaying && player.ShouldResumePlay() )
                            {
                                player.ResumePlay();
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }
    };

}