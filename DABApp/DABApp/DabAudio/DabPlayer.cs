using System;
using System.IO;
using System.Net;
using Plugin.SimpleAudioPlayer;

namespace DABApp.DabAudio
{

    //Class that extends the basic player used by the apps.

    public class DabPlayer : ISimpleAudioPlayer
    {
        private ISimpleAudioPlayer player;
        private string _channelTitle = "";
        private string _episodeTitle = "";

        public DabPlayer(ISimpleAudioPlayer Player)
        {
            player = Player;
        }

        public double Duration
        {
            get
            {
                //Return the duration of the player, ensuring it's >0
                if (player.Duration <= 0)
                { return 1; }
                else
                { return player.Duration; }
            }
        }

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
            //Load file, determine local or remote first
            //If remote, use a stream.
            if (fileName.ToLower().StartsWith("http", StringComparison.Ordinal))
            {
                //Remote file
                WebClient wc = new WebClient();
                Stream fileStream = wc.OpenRead(fileName);
                return player.Load(fileStream);
            } else
            {
                //Local file
                return player.Load(fileName);

            }

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

        public bool IsReady
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

        //Title of the channel
        public string ChannelTitle => _channelTitle;

        //Title of the episode
        public string EpisodeTitle => _episodeTitle;

        //Path to use for the play/pause image based on the state of the player
        public string PlayPauseButtonImageBig
        {
            //TODO: Implement this.
            get { return ""; }
        }
    }
}
