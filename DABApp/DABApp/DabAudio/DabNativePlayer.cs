using System;
using Plugin.SimpleAudioPlayer;

namespace DABApp.DabAudio
{



    public interface IDabNativePlayer
    {
        void Init(DabPlayer Player, bool IntegrateWithLockScreen); //Init the player on the platform-specific implementation
    }
}
