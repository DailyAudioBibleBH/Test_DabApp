using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;

namespace DABApp.DabAudio
{

    public class DabPlayerEventArgs : EventArgs
    {
        public string ChannelTitle;
        public string EpisodeTitle;
        public double Duration;
        public double CurrentPosition;

        public DabPlayerEventArgs(DabPlayer player)
        {
            //Init the object with known values
            ChannelTitle = player.ChannelTitle;
            EpisodeTitle = player.EpisodeTitle;
            Duration = player.Duration;
            CurrentPosition = player.CurrentPosition;
        }
    }

    //Class that extends the basic player used by the apps.

    public class DabPlayer :  INotifyPropertyChanged
    {
        private IDabNativePlayer nativePlayer;
        private string _channelTitle = "";
        private string _episodeTitle = "";
        private double _episodeDuration = 1; //Estimated duration of the episode
        private Timer timer = new Timer(500);
        private double LastPosition = 0;




        //Constructor 
        public DabPlayer( bool IntegrateWithLockScreen)
        {

            
            
            //Connect the native player interface
            nativePlayer = DependencyService.Get<IDabNativePlayer>();
            nativePlayer.Init(this, IntegrateWithLockScreen);
            //Set up events tied to the player
            nativePlayer.PlaybackEnded += Player_PlaybackEnded;



            //Set up the timer for tracking progress
            timer.Elapsed += OnTimerFired;
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Stop(); //Don't use it till we need it.
        }
        

        /* Event Handlers */
        public event EventHandler EpisodeDataChanged;
        protected virtual void OnEpisodeDataChanged(object sender, DabPlayerEventArgs e)
        {
            EventHandler handler = EpisodeDataChanged;
            handler?.Invoke(this, new DabPlayerEventArgs(this));
        }

        public event EventHandler EpisodeProgressChanged;
        protected virtual void OnEpisodeProgressChanged(object sender, DabPlayerEventArgs e)
        {
            EventHandler handler = EpisodeProgressChanged;
            handler?.Invoke(this, new DabPlayerEventArgs(this));
        }




        void Player_PlaybackEnded(object sender, EventArgs e)
        {
            //Handle playback ending (update button image)
            OnPropertyChanged("PlayPauseButtonImageBig");
        }


        /********************************
        ISimpleAudioPlayer Implementation 
        ********************************/

        public double Duration
        {
            get
            {
                //Return the duration of the player, ensuring it's >0
                if (nativePlayer.Duration <= 0)
                {
                    //TODO: Use the episodes total length if possible
                    return _episodeDuration;
                }
                else
                {
                    return nativePlayer.Duration;
                }
            }
        }

        //Current position of the player
        public double CurrentPosition
        {
            get
            {
                return nativePlayer.CurrentPosition;
            }
        }

        //Remaining time for the player
        public double RemainingSeconds
        {
            get
            {
                return nativePlayer.Duration - CurrentPosition;
            }
        }

        //Current position of the player as a percentage
        public double CurrentProgressPercentage
        {
            get
            {
                if (Duration > 0)
                {
                    return CurrentPosition / Duration;
                }
                else
                {
                    return 0;
                }
            }
        }

        public double Volume
        {
            get
            {
                return nativePlayer.Volume;

            }
            set
            {
                nativePlayer.Volume = value;
                OnPropertyChanged("Volume");
            }
        }
        public double Balance
        {
            get
            {
                return nativePlayer.Balance;
            }
            set
            {
                nativePlayer.Balance = value;
                OnPropertyChanged("Balance");
            }
        }

        public bool IsPlaying => nativePlayer.IsPlaying;

        public bool Loop
        {
            get
            {
                return nativePlayer.Loop;
            }
            set
            {
                nativePlayer.Loop = value;
                OnPropertyChanged("Loop");
            }
        }

        public bool CanSeek => nativePlayer.CanSeek;

        public event EventHandler PlaybackEnded
        {
            add
            {
                nativePlayer.PlaybackEnded += value;
            }

            remove
            {
                nativePlayer.PlaybackEnded -= value;
            }
        }

        public void Dispose()
        {
            //nativePlayer.Dispose();
        }


        public bool Load(Stream audioStream)
        {
            //Load a stream
            Task.Run(() =>
            {
                bool rv = nativePlayer.Load(audioStream);
                OnPropertyChanged("Duration");
            }
            );

            OnPropertyChanged("IsReady");
            return true;
        }


        public bool Load(string fileName)
        {

            //Load file, determine local or remote first
            //If remote, use a stream.
            bool rv;

            if (fileName.ToLower().StartsWith("http", StringComparison.Ordinal))
            {
                rv = nativePlayer.LoadUrl(fileName);
            }
            else
            {
                //Local file
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                rv = Load(fs);
            }
            return rv;

        }

        public bool Load(dbEpisodes episode)
        {
            try
            {
                //Stop playing the current episode if needed
                if (IsPlaying)
                {
                    Pause(); //Have to use PAUSE on Android or it will reset current time to 0.
                    //Stop();
                }

                //Load a specific episode (sets text properties as well
                EpisodeTitle = episode.title;
                ChannelTitle = episode.channel_title;

                OnEpisodeDataChanged(this, new DabPlayerEventArgs(this));
                return Load(episode.File_name);
            }
            catch (Exception ex)
            {
                return false;
            }


        }


        public void Pause()
        {
            nativePlayer.Pause();
            timer.Stop();
            OnPropertyChanged("PlayPauseButtonImageBig");

            UpdateEpisodeDataOnStop(); //Episode has been stopped
        }

        public void Play()
        {
            if (IsPlaying)
            {
                //Stop the current episode properly if needed.
                Stop();
            }

            nativePlayer.Play();
            timer.Start();
            OnPropertyChanged("PlayPauseButtonImageBig");
        }

        public void Seek(double position)
        {
            nativePlayer.Seek(position);
            OnPropertyChanged("CurrentPosition");
            OnPropertyChanged("RemainingSeconds");
            OnPropertyChanged("CurrentProgressPercentage");
        }

        public void Stop()
        {
            nativePlayer.Stop();
            timer.Stop();
            OnPropertyChanged("PlayPauseButtonImageBig");

            UpdateEpisodeDataOnStop(); //episode has stopped

        }





        /******************************************
         * INotifyPropertyChanged Implementation
         *******************************************/

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /* Timer for updating progress */

        private void OnTimerFired(object sender, ElapsedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(DateTime.Now.Ticks);
            //Raise an event if the players progress has moved
            if (LastPosition != nativePlayer.CurrentPosition)
            {
                LastPosition = nativePlayer.CurrentPosition;
                OnPropertyChanged("CurrentPosition");
                OnPropertyChanged("RemainingSeconds");
                OnPropertyChanged("CurrentProgressPercentage");

                //Fire event to tell native players to update lock screen
                OnEpisodeProgressChanged(this, new DabPlayerEventArgs(this)); //Notify lock screen of new position
            }
        }



        /******************************************
        /* Custom Properties added to our player */
        /****************************************/

        private void UpdateEpisodeDataOnStop()
        {
            //Call other methods related to stopping / pausing an episode
            int e = GlobalResources.CurrentEpisodeId;
            PlayerFeedAPI.UpdateStopTime(e, CurrentPosition, RemainingSeconds);
            AuthenticationAPI.CreateNewActionLog(e, "pause", CurrentPosition, null, null);

        }

        public void Skip(double seconds)
        {
            //Skip forward or backward from current position
            double newPosition = nativePlayer.CurrentPosition + seconds;
            if (newPosition < 0) { newPosition = 0; }
            if (newPosition > nativePlayer.Duration)
            {
                newPosition = nativePlayer.Duration;
            }
            Seek(newPosition);
        }

        public bool IsReady
        {
            get
            {
                //Return if the player is ready to go.
                if (GlobalResources.CurrentEpisodeId >0)
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
        public string ChannelTitle
        {
            get
            {
                return _channelTitle;
            }
            set
            {
                _channelTitle = value;
                OnPropertyChanged("ChannelTitle");
            }
        }

        //Title of the episode
        public string EpisodeTitle
        {
            get
            {
                return _episodeTitle;
            }
            set
            {
                _episodeTitle = value;
                OnPropertyChanged("EpisodeTitle");
            }
        }

        /* Image Resources */

        //Path to use for the play/pause image based on the state of the player
        public ImageSource PlayPauseButtonImageBig
        {
            get
            {
                if (IsPlaying)
                {
                    //Pause button
                    return ImageSource.FromFile("ic_pause_circle_outline_white_3x.png");
                }
                else
                {
                    //Play button
                    return ImageSource.FromFile("ic_play_circle_outline_white_3x.png");
                }
            }
        }


    }
}
