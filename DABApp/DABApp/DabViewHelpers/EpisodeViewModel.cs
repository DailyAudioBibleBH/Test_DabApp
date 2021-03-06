using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
    public class EpisodeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool unTouched;
        private bool noProgress;
        private double progress = -.01;
        public dbEpisodes Episode { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string channelTitle { get; set; }
        public string notes { get; set; }
        public bool isDownloaded { get; set; }
        public bool isNotDownloaded { get; set; }

        public EpisodeViewModel(dbEpisodes episode)
        {
            Episode = episode;
            description = episode.description;
            notes = episode.notes;
            title = episode.title;
            channelTitle = episode.channel_title;
            noProgress = episode.is_downloaded;
            isDownloaded = episode.is_downloaded;
            isNotDownloaded = !isDownloaded;
            FileManager fm = new FileManager();
            fm.EpisodeDownloading += UpdateDownload;
            PlayerFeedAPI.MakeProgressVisible += DownloadStarted;
        }

        public bool downloadVisible
        {
            get
            {
                return Episode.is_downloaded;
            }
            set
            {
                Episode.is_downloaded = value;
                isDownloaded = value;
                isNotDownloaded = !value;
                OnPropertyChanged("downloadVisible");
            }
        }

        public double downloadProgress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = value;
                OnPropertyChanged("downloadProgress");
            }
        }

        public bool progressVisible
        {
            get
            {
                return Episode.progressVisible;
            }
            set
            {
                Episode.progressVisible = value;
                OnPropertyChanged("progressVisible");
            }
        }

        public bool IsListenedTo
        {
            //check this
            get
            {
                return unTouched = Episode.UserData.IsListenedTo;
            }
            set
            {
                unTouched = value;
                var data = Episode.UserData;
                data.IsListenedTo = value;
                data.Save();
                OnPropertyChanged("listenedToSource");
                OnPropertyChanged("IsListenedTo");
                OnPropertyChanged("listenAccessible");
            }
        }

        public string listenAccessible
        {
            get
            {
                return IsListenedTo ? "listen to status Completed" : "listen to status not completed";
            }
            set { throw new Exception("You cannot set this directly"); }
        }

        public bool IsFavorite
        {
            get
            {
                return Episode.UserData.IsFavorite;
            }
            set
            {
                var data = Episode.UserData;
                data.IsFavorite = value;
                data.Save();
                OnPropertyChanged("IsFavorite");
                OnPropertyChanged("favoriteSource");
                OnPropertyChanged("favoriteAccessible");
            }
        }

        public ImageSource favoriteSource
        {
            get
            {
                //TODO Simplify these graphics with vectors or other resource that doesn't need so many file names
                //Return the appropiate image representing if an episode is a favorite or not
                if (Device.RuntimePlatform == Device.iOS || Device.Idiom == TargetIdiom.Tablet)
                {
                    if (Episode.UserData.IsFavorite)
                    {
                        return ImageSource.FromFile("ic_star_white_3x.png");
                    }
                    else
                    {
                        return ImageSource.FromFile("ic_star_border_white_3x.png");
                    }
                }
                else
                {
                    if (Episode.UserData.IsFavorite)
                    {
                        return ImageSource.FromFile("ic_star_white.png");
                    }
                    else
                    {
                        return ImageSource.FromFile("ic_star_border_white.png");
                    }

                }

            }
        }

        public string favoriteAccessible
        {
            get
            {
                return Episode.UserData.IsFavorite ? "favorite status favorited" : "favorite status not favorited";
            }
            set { throw new Exception("You cannot set this directly"); }
        }

        public ImageSource listenedToSource
        {
            get
            {
                if (IsListenedTo)
                {
                    return ImageSource.FromFile("ic_check_box_teal_3x.png");
                }
                else
                {

                    return ImageSource.FromFile("ic_check_box_outline_blank_white_3x.png");
                }
            }
        }

        public bool HasJournal
        {
            get
            {
                return Episode.UserData.HasJournal;
            }
            set
            {
                Episode.UserData.HasJournal = value;
                Episode.UserData.Save();
                OnPropertyChanged("HasJournal");
            }
        }

        void UpdateDownload(object o, DabEventArgs e)
        {
            if (Episode.id.Value == e.EpisodeId)
            {
                downloadVisible = true;
                downloadProgress = e.ProgressPercentage * 100;
            }
        }

        void DownloadStarted(object o, DabEventArgs e)
        {
            if (e.EpisodeId == Episode.id.Value)
            {
                progressVisible = true;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

        private static void NotifyStaticPropertyChanged(string propertyName)
        {
            StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}