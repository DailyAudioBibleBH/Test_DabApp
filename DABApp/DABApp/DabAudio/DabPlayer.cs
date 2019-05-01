﻿using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Timers;
using Plugin.SimpleAudioPlayer;
using Xamarin.Forms;

namespace DABApp.DabAudio
{

    //Class that extends the basic player used by the apps.

    public class DabPlayer : ISimpleAudioPlayer, INotifyPropertyChanged
    {
        private ISimpleAudioPlayer player;
        private string _channelTitle = "";
        private string _episodeTitle = "";
        private Timer timer = new Timer(500);
        private double LastPosition = 0;

        //Constructor 
        public DabPlayer(ISimpleAudioPlayer Player)
        {
            player = Player;

            player.PlaybackEnded += Player_PlaybackEnded;



            //Set up the timer for tracking progress
            timer.Elapsed += OnTimerFired;
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Stop(); //Don't use it till we need it.
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
                if (player.Duration <= 0)
                {
                    return 1;
                }
                else
                {
                    return player.Duration;
                }
            }
        }

        public double CurrentPosition
        {
            get
            {
                return player.CurrentPosition;
            }
        }

        public double Volume
        {
            get
            {
                return player.Volume;

            }
            set
            {
                player.Volume = value;
                OnPropertyChanged("Volume");
            }
        }
        public double Balance
        {
            get
            {
                return player.Balance;
            }
            set
            {
                player.Balance = value;
                OnPropertyChanged("Balance");
            }
        }

        public bool IsPlaying => player.IsPlaying;

        public bool Loop
        {
            get
            {
                return player.Loop;
            }
            set
            {
                player.Loop = value;
                OnPropertyChanged("Loop");
            }
        }

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
            //Load a stream
            bool rv = player.Load(audioStream);
            OnPropertyChanged("Duration");
            return rv;
        }

        public bool Load(string fileName)
        {
            //Load file, determine local or remote first
            //If remote, use a stream.
            bool rv;

            if (fileName.ToLower().StartsWith("http", StringComparison.Ordinal))
            {
                //Remote file
                WebClient wc = new WebClient();
                Stream fileStream = wc.OpenRead(fileName);
                rv = player.Load(fileStream);
            }
            else
            {
                //Local file
                rv= player.Load(fileName);
            }
            OnPropertyChanged("Duration");
            return rv;

        }


        public void Pause()
        {
            player.Pause();
            timer.Stop();
            OnPropertyChanged("PlayPauseButtonImageBig");
        }

        public void Play()
        {
            player.Play();
            timer.Start();
            OnPropertyChanged("PlayPauseButtonImageBig");
        }

        public void Seek(double position)
        {
            player.Seek(position);
            OnPropertyChanged("CurrentPosition");
        }

        public void Stop()
        {
            player.Stop();
            timer.Stop();
            OnPropertyChanged("PlayPauseButtonImageBig");
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
            if (LastPosition != player.CurrentPosition)
            {
                LastPosition = player.CurrentPosition;
                OnPropertyChanged("CurrentPosition");
            }

        }



        /******************************************
        /* Custom Properties added to our player */
        /****************************************/

        public void Skip(double seconds)
        {
            //Skip forward or backward from current position
            double newPosition = player.CurrentPosition + seconds;
            if (newPosition < 0) { newPosition = 0; }
            if (newPosition > player.Duration)
            {
                newPosition = player.Duration;
            }
            Seek(newPosition);
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
