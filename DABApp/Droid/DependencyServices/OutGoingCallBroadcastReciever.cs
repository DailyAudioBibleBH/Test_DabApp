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

namespace DABApp.Droid.DependencyServices
{
    //[BroadcastReceiver(Name = "com.test.OutgoingCallBroadcastReceiver")]
    //[IntentFilter(new[] { Intent.ActionNewOutgoingCall, TelephonyManager.ActionPhoneStateChanged })]
    public class OutgoingCallBroadcastReceiver : BroadcastReceiver
    {

        //public override void OnReceive(Context context, Intent intent)
        //{
        //    switch (intent.Action)
        //    {
        //        case Intent.ActionNewOutgoingCall:
        //            var outboundPhoneNumber = intent.GetStringExtra(Intent.ExtraPhoneNumber);
        //            Toast.MakeText(context, $"Started: Outgoing Call to {outboundPhoneNumber}", ToastLength.Long).Show();
        //            break;
        //        case TelephonyManager.ActionPhoneStateChanged:
        //            var state = intent.GetStringExtra(TelephonyManager.ExtraState);
        //            if (state == TelephonyManager.ExtraStateIdle)
        //                Toast.MakeText(context, "Phone Idle (call ended)", ToastLength.Long).Show();
        //            else if (state == TelephonyManager.ExtraStateOffhook)
        //                Toast.MakeText(context, "Phone Off Hook", ToastLength.Long).Show();
        //            else if (state == TelephonyManager.ExtraStateRinging)
        //                Toast.MakeText(context, "Phone Ringing", ToastLength.Long).Show();
        //            else if (state == TelephonyManager.ExtraIncomingNumber)
        //            {
        //                var incomingPhoneNumber = intent.GetStringExtra(TelephonyManager.ExtraIncomingNumber);
        //                Toast.MakeText(context, $"Incoming Number: {incomingPhoneNumber}", ToastLength.Long).Show();
        //            }
        //            break;
        //        default:
        //            break;
        //    }
        //}
        //The receiver will be recreated whenever android feels like it.  We need a static variable to remember data between instantiations

        private static string lastState = TelephonyManager.ExtraStateIdle;
        private static DateTime callStartTime;
        private static bool isIncoming;
        private static String savedNumber;  //because the passed incoming is only valid in ringing

        public override void OnReceive(Context context, Intent intent)
        {
            //We listen to two intents.  The new outgoing call only tells us of an outgoing call.  We use it to get the number.
            if (intent.Action.Equals("android.intent.action.NEW_OUTGOING_CALL"))
            {
                savedNumber = intent.Extras.GetString("android.intent.extra.PHONE_NUMBER");
            }
            else
            {
                String stateStr = intent.Extras.GetString(TelephonyManager.ExtraState);
                String number = intent.Extras.GetString(TelephonyManager.ExtraIncomingNumber);
                string state = "0";
                if (stateStr.Equals(TelephonyManager.ExtraStateIdle))
                {
                    state = TelephonyManager.ExtraStateIdle;
                }
                else if (stateStr.Equals(TelephonyManager.ExtraStateOffhook))
                {
                    state = TelephonyManager.ExtraStateOffhook;
                }
                else if (stateStr.Equals(TelephonyManager.ExtraStateRinging))
                {
                    state = TelephonyManager.ExtraStateRinging;
                }


                onCallStateChanged(context, state, number);
            }
        }

        //Derived classes should override these to respond to specific events of interest
        protected void onIncomingCallReceived(Context ctx, String number, DateTime start)
        {
            System.Diagnostics.Debug.WriteLine("Call recieved");
        }
        protected void onIncomingCallAnswered(Context ctx, String number, DateTime start)
        {

        }
        protected void onIncomingCallEnded(Context ctx, String number, DateTime start, DateTime end)
        {

        }

        protected void onOutgoingCallStarted(Context ctx, String number, DateTime start)
        {

        }
        protected void onOutgoingCallEnded(Context ctx, String number, DateTime start, DateTime end)
        {

        }

        protected void onMissedCall(Context ctx, String number, DateTime start)
        {

        }

        //Deals with actual events

        //Incoming call-  goes from IDLE to RINGING when it rings, to OFFHOOK when it's answered, to IDLE when its hung up
        //Outgoing call-  goes from IDLE to OFFHOOK when it dials out, to IDLE when hung up
        public void onCallStateChanged(Context context, string state, string number)
        {
            if (lastState == state)
            {
                //No change, debounce extras
                return;
            }
            switch (state)
            {
                case TelephonyManager.ActionPhoneStateChanged:
                    isIncoming = true;
                    callStartTime = new DateTime();
                    savedNumber = number;
                    onIncomingCallReceived(context, number, callStartTime);
                    //break;
                    //case TelephonyManager.ActionPhoneStateChanged:
                    //Transition of ringing->offhook are pickups of incoming calls.  Nothing done on them
                    if (lastState != TelephonyManager.ExtraStateRinging)
                    {
                        isIncoming = false;
                        callStartTime = new DateTime();
                        onOutgoingCallStarted(context, savedNumber, callStartTime);
                    }
                    else
                    {
                        isIncoming = true;
                        callStartTime = new DateTime();
                        onIncomingCallAnswered(context, savedNumber, callStartTime);
                    }

                    //break;
                //case TelephonyManager.CALL_STATE_IDLE:
                    //Went to idle-  this is the end of a call.  What type depends on previous state(s)
                    if (lastState == TelephonyManager.ExtraStateRinging)
                    {
                        //Ring but no pickup-  a miss
                        onMissedCall(context, savedNumber, callStartTime);
                    }
                    else if (isIncoming)
                    {
                        onIncomingCallEnded(context, savedNumber, callStartTime, new DateTime());
                    }
                    else
                    {
                        onOutgoingCallEnded(context, savedNumber, callStartTime, new DateTime());
                    }
                    break;
            }
            lastState = state;
        }
    }    
}