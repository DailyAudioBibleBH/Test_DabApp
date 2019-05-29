using System;
using Android.Content;
using DABApp.DabAudio;
using DABApp.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(droidDabNativePlayer))]
namespace DABApp.Droid
{
    public class droidDabNativePlayer : IDabNativePlayer
    {
        public void Init(DabPlayer Player, bool IntegrateWithLockScreen)
        {
            throw new NotImplementedException();
        }
    }
}
