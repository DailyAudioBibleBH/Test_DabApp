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
using Plugin.SimpleAudioPlayer;

namespace DABApp.Droid
{
    [BroadcastReceiver]
    [Android.App.IntentFilter(new[] { Intent.ActionMediaButton })]
    public class RemoteControlBroadcastReceiver : BroadcastReceiver
    {
        public string ComponentName { get { return this.Class.Name; } }
        DabPlayer player = GlobalResources.playerPodcast;
        DabPlayer player2 = new DabPlayer(simpleAudioPlayer, true);


        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Intent.ActionMediaButton)
                return;

            //The event will fire twice, up and down.
            // we only want to handle the down event though.
            var key = (KeyEvent)intent.GetParcelableExtra(Intent.ExtraKeyEvent);
            if (key.Action != KeyEventActions.Down)
                return;
            var action = StreamingBackgroundService.ActionPlay;
            switch (key.KeyCode)
            {
                case Keycode.Headsethook:
                case Keycode.MediaPlayPause: action = StreamingBackgroundService.ActionTogglePlayback; break;
                case Keycode.MediaPlay: action = StreamingBackgroundService.ActionPlay; break;
                case Keycode.MediaPause: action = StreamingBackgroundService.ActionPause; break;
                case Keycode.MediaStop: action = StreamingBackgroundService.ActionStop; break;
                case Keycode.MediaNext: action = StreamingBackgroundService.ActionNext; break;
                case Keycode.MediaPrevious: action = StreamingBackgroundService.ActionPrevious; break;
                default: return;
            }
            var remoteIntent = new Intent(action);
            context.StartService(remoteIntent);
        }
    }
}