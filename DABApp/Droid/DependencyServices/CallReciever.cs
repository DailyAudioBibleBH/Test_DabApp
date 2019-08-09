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
    public class CallReciever : PhoneStateListener
    {
        TelephonyManager telManager;
        Context context;
        DabPlayer player;

        //public override void OnReceive(Context context, Intent intent)
        //{
        //    this.context = context;

        //    telManager = (TelephonyManager)context.GetSystemService(Context.TelephonyService);
        //    telManager.Listen(phoneListener, PhoneStateListener.LISTEN_CALL_STATE);
        //}

        
        public override void OnCallStateChanged([GeneratedEnum] CallState state, string incomingNumber)
        {
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
                                    player.Pause();
                                }
                            }
                            //PAUSE
                            break;
                        }
                    case CallState.Offhook:
                        {

                            break;
                        }
                    case CallState.Idle:
                        {
                            if (player.IsReady)
                            {
                                if (player.IsPlaying)
                                {
                                    player.Play();
                                }
                            }
                            //PLAY
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

