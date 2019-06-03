using System;
using Android.Content;
using DABApp.DabAudio;
using DABApp.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(DroidDabNativePlayer))]
namespace DABApp.Droid
{

    public class DroidDabNativePlayer : MediaSession.Callback, IDabNativePlayer
    {

        DabPlayer player;

        public DroidDabNativePlayer()
        {
        }

        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {
            player = Player;


            if (IntegrateWithLockScreen)
            {
                /* SET UP LOCK SCREEN */
                //TODO: Set up lock screen

            }


        }


    }
}
