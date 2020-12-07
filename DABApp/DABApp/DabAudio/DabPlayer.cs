using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using SQLite;
using Xamarin.Forms;
using static DABApp.Service.DabService;

namespace DABApp.DabAudio
{

    public class DabPlayerEventArgs : EventArgs
    {
        public string ChannelTitle;
        public string EpisodeTitle;
        public string EpisodeDescription;
        public double Duration;
        public double CurrentPosition;

        public DabPlayerEventArgs(DabPlayer player)
        {
            //Init the object with known values
            ChannelTitle = player.ChannelTitle;
            EpisodeTitle = player.EpisodeTitle;
            EpisodeDescription = player.EpisodeDescription;
            Duration = player.Duration;
            CurrentPosition = player.CurrentPosition;
        }
    }

    //Class that extends the basic player used by the apps.

    public class DabPlayer : INotifyPropertyChanged
    {
        private IDabNativePlayer nativePlayer;
        private string _channelTitle = "";
        private string _episodeTitle = "";
        private string _episodeDescription = "";
        private double _episodeDuration = 1; //Estimated duration of the episode
        private Timer timer = new Timer(500);
        private double LastPosition = 0;
        private bool shouldResumePlay = false;
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        private double lastLogPlayerPosition;




        //Constructor 
        public DabPlayer(bool IntegrateWithLockScreen)
        {



            //Connect the native player interface
            //nativePlayer = DependencyService.Get<IDabNativePlayer>();
            nativePlayer = DependencyService.Get<IDabNativePlayer>(DependencyFetchTarget.NewInstance);

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
            //Go back to the beginning
            //Seek(0);
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
                try
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
                catch (Exception ex)
                {
                    return 0;
                }
                
            }
        }

        //Current position of the player
        public double CurrentPosition
        {
            get
            {
                try
                {
                    return nativePlayer.CurrentPosition;
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
            set
            {
                //do nothing - player position can't be set directly.
            }
        }

        //Remaining time for the player
        public double RemainingSeconds
        {
            get
            {
                try
                {
                    return nativePlayer.Duration - CurrentPosition;
                }
                catch (Exception ex)
                {
                    return 0;
                }
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
                try
                {
                    return nativePlayer.Volume;
                }
                catch (Exception ex)
                {
                    return 0;
                }

            }
            set
            {
                try
                {
                    nativePlayer.Volume = value;
                    OnPropertyChanged("Volume");
                }
                catch (Exception ex)
                {
                }
            }
        }


        public bool IsPlaying
        {
            get
            {
                try
                {
                    return nativePlayer.IsPlaying;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        public bool CanSeek
        {
            get
            {
                try
                {
                    return nativePlayer.CanSeek;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

        }

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

        public bool Load(string fileName)
        {

            //Load file, determine local or remote first
            //If remote, use a stream.
            bool rv;
            rv = nativePlayer.Load(fileName);
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
                EpisodeDescription = episode.description;
                ChannelTitle = episode.channel_title;

                OnEpisodeDataChanged(this, new DabPlayerEventArgs(this));
                return Load(episode.File_name);
            }
            catch (Exception ex)
            {
                return false;
            }


        }

        //Method that can be called from outside the player to notify bound elements to update their status.
        public void NotifyPlayStateChanged()
        {
            OnPropertyChanged("PlayPauseButtonImageBig");
        }

        public void Pause()
        {
            UpdateEpisodeDataOnStop(); //Episode has been stopped
            nativePlayer.Pause();
            timer.Stop();
            OnPropertyChanged("PlayPauseButtonImageBig");
        }

        public void PlayPauseBluetooth()
        {

            try
            {

                if (IsPlaying)
                {
                    nativePlayer.Pause();
                }
                else
                {
                    nativePlayer.Play();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void Play()
        {

            try
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
            catch (Exception ex)
            {

            }
        }

        public void Seek(double position)
        {
            try
            {
                nativePlayer.Seek(position);
            }
            catch (Exception ex)
            {

            }
        }

        public void Stop()
        {
            try
            {
                nativePlayer.Stop();
                timer.Stop();
                OnPropertyChanged("PlayPauseButtonImageBig");

                UpdateEpisodeDataOnStop(); //episode has stopped
            }
            catch (Exception ex)
            {

            }

        }





        /******************************************
         * INotifyPropertyChanged Implementation
         *******************************************/

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /* Timer for updating progress */

        private void OnTimerFired(object sender, ElapsedEventArgs e)
        {
            try
            {
                //Raise an event if the players progress has moved
                if (LastPosition != nativePlayer.CurrentPosition)
                {
                    LastPosition = nativePlayer.CurrentPosition;
                    OnPropertyChanged("CurrentPosition");
                    OnPropertyChanged("RemainingSeconds");
                    OnPropertyChanged("CurrentProgressPercentage");

                    //Fire event to tell native players to update lock screen
                    OnEpisodeProgressChanged(this, new DabPlayerEventArgs(this)); //Notify lock screen of new position

                    //Log position change to an action log
                    var difference = Math.Abs(lastLogPlayerPosition - CurrentPosition);
                    if (lastLogPlayerPosition != CurrentPosition && difference >= ContentConfig.Instance.options.log_position_interval)
                    {
                        Debug.WriteLine($"Logging progress...{DateTime.Now}");
                        _ = AuthenticationAPI.CreateNewActionLog(GlobalResources.CurrentEpisodeId, Service.DabService.ServiceActionsEnum.PositionChanged, CurrentPosition, null, null, null);
                        lastLogPlayerPosition = CurrentPosition;
                    }

                }
            }
            catch (Exception ex)
            {

            }
        }



        /******************************************
        /* Custom Properties added to our player */
        /****************************************/

        private void UpdateEpisodeDataOnStop()
        {
            try
            {
                //Call other methods related to stopping / pausing an episode
                int e = GlobalResources.CurrentEpisodeId;
                if (e > 0)
                {
                    PlayerFeedAPI.UpdateStopTime(e, CurrentPosition, RemainingSeconds);
                    AuthenticationAPI.CreateNewActionLog(e, ServiceActionsEnum.PositionChanged, CurrentPosition, null, null);
                    lastLogPlayerPosition = CurrentPosition;
                }
            }
            catch (Exception ex)
            {

            }

        }

        public void Skip(double seconds)
        {
            try
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
            catch (Exception ex)
            {

            }
        }

        public bool IsReady
        {
            get
            {
                try
                {
                    //Return if the player is ready to go.
                    if (GlobalResources.CurrentEpisodeId > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
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

        public string EpisodeDescription
        {
            get
            {
                return _episodeDescription;
            }
            set
            {
                _episodeDescription = value;
                OnPropertyChanged("EpisodeDescription");
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

        public bool ShouldResumePlay()
        {
            return shouldResumePlay;
        }

        public void ResumePlay()
        {
            shouldResumePlay = false;
            Play();
        }

        //Pause method for if a call was interrupted 
        public void PauseForCall()
        {
            shouldResumePlay = true;
            Pause();
        }
    }
}
