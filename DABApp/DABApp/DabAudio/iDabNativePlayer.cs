using System;
using System.IO;

namespace DABApp.DabAudio
{



    public interface IDabNativePlayer
    {

        /*
         * Events
         * */

        /// Raised when audio playback completes successfully 
        event EventHandler PlaybackEnded;

        /*
         * Properties
         * */

        /// Length of audio in seconds
        double Duration { get; }

        /// Current position of audio playback in seconds
        double CurrentPosition { get; }

        /// Playback volume 0 to 1 where 0 is no-sound and 1 is full volume
        double Volume { get; set; }

        /// Indicates if the currently loaded audio file is playing
        bool IsPlaying { get; }

        /// Indicates if the position of the loaded audio file can be updated
        bool CanSeek { get; }

        /*
         * Methods
         * */

        //Init the player with a DabPlayer class for interaction with the app
        void Init(DabPlayer Player, bool IntegrateWithLockScreen); //Init the player on the platform-specific implementation

        //Load audio file from url or local file path
        bool Load(string path);

        /// Begin playback or resume if paused
        void Play();

        /// Pause playback if playing (does not resume)
        void Pause();

        /// Stop playack and set the current position to the beginning
        void Stop();

        /// Set the current playback position (in seconds)
        void Seek(double position);

       
    }
}
