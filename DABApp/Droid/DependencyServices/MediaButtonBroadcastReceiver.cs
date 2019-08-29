using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.DabAudio;

namespace DABApp.Droid.DependencyServices
{
    [BroadcastReceiver] //(Enabled = true, Exported = false)
    [IntentFilter(new[] { "android.intent.action.MEDIA_BUTTON" })] // , BluetoothHeadset.ActionAudioStateChanged, BluetoothHeadset.ActionVendorSpecificHeadsetEvent
    public class MediaButtonBroadcastReceiver : BroadcastReceiver
    {
        public string ComponentName { get { return Class.Name; } }
        public static int trackReciever; 

        public override void OnReceive(Context context, Intent intent)
        {
            trackReciever++;
            DabPlayer player = GlobalResources.playerPodcast;
            if (trackReciever % 2 != 0)
            {
                if (intent.Action != Intent.ActionMediaButton)
                    return;

                var keyEvent = (KeyEvent)intent.GetParcelableExtra(Intent.ExtraKeyEvent);

                switch (keyEvent.KeyCode)
                {
                    case Keycode.MediaPlay:
                        player.Play();
                        break;
                    case Keycode.MediaPause:
                        player.Pause();
                        break;
                    case Keycode.MediaPlayPause:
                        player.PlayPauseBluetooth();
                        break;
                    case Keycode.MediaNext:
                        player.Seek(player.CurrentPosition + 30);
                        break;
                    case Keycode.MediaPrevious:
                        player.Seek(player.CurrentPosition - 30);
                        break;
                }
            }
        }
    }
}