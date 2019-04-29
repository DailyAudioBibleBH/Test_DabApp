using System;
using System.IO;
using Plugin.SimpleAudioPlayer;

namespace DABApp.DabAudio
{

    //Class that extends the basic player used by the apps.

    public class DabPlayer : ISimpleAudioPlayer
    {
        private ISimpleAudioPlayer player;

        public DabPlayer(ISimpleAudioPlayer Player)
        {
            player = Player;
        }

        public double Duration => player.Duration;

        public double CurrentPosition => player.CurrentPosition;

        public double Volume { get => player.Volume; set => player.Volume = value; }
        public double Balance { get => player.Balance; set => player.Balance = value; }

        public bool IsPlaying => player.IsPlaying;

        public bool Loop { get => player.Loop; set => player.Loop = value; }

        public bool CanSeek => player.CanSeek;

        public event EventHandler PlaybackEnded
        {
            add
            {
                player.PlaybackEnded += value;
            }

            remove
            {
                player.PlaybackEnded -= value;
            }
        }

        public void Dispose()
        {
            player.Dispose();
        }

        public bool Load(Stream audioStream)
        {
            return player.Load(audioStream);
        }

        public bool Load(string fileName)
        {
            return player.Load(fileName);
        }

        public void Pause()
        {
            player.Pause();
        }

        public void Play()
        {
            player.Play();
        }

        public void Seek(double position)
        {
            player.Seek(position);
        }

        public void Stop()
        {
            player.Stop();
        }

        /* Custom Properties added to our player */

        public void Skip(double seconds)
        {
            //Skip forward or backward from current position
            double newPosition = player.CurrentPosition + seconds;
            if (newPosition < 0) { newPosition = 0; }
            if (newPosition > player.Duration) { newPosition = player.Duration; }
            player.Seek(newPosition);
        }

        public bool isReady
        {
            get
            {
                //Return if the player is ready to go.
                //TODO: Make this more intelligent than just looking for duration >0
                if (player.Duration > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SwitchOutputs()
        {
            //TODO: Impelement this method
            throw new NotImplementedException();
        }
    }
}
