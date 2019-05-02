using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public EpisodeViewModel(dbEpisodes episode)
        {
            Episode = episode;
            description = episode.description;
            title = episode.title;
            channelTitle = episode.channel_title;
            noProgress = episode.is_downloaded;
            DependencyService.Get<IFileManagement>().EpisodeDownloading += UpdateDownload;
            DependencyService.Get<IFileManagement>().EpisodeCompleted += DownloadComplete;
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

        public bool listenedToVisible
        {
            get
            {
                return unTouched = Episode.is_listened_to == "listened" ? true : false;
            }
            set
            {
                unTouched = value;
                Episode.is_listened_to = unTouched ? "listened" : "";
                OnPropertyChanged("listenedToSource");
                OnPropertyChanged("listenedToVisible");
                OnPropertyChanged("listenAccessible");
            }
        }

        public string listenAccessible
        {
            get
            {
                return listenedToVisible ? "listen to status Completed": "listen to status not completed";
            }
            set { throw new Exception("You cannot set this directly"); }
        }

        public bool favoriteVisible
        {
            get
            {
                return Episode.is_favorite;
            }
            set
            {
                Episode.is_favorite = value;
                OnPropertyChanged("favoriteVisible");
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
                    if (Episode.is_favorite)
                    {
                        return ImageSource.FromFile("ic_star_white_3x.png");
                    }
                    else {
                        return ImageSource.FromFile("ic_star_border_white_3x.png");
                    }
                }
                else
                {
                    if (Episode.is_favorite)
                    {
                        return ImageSource.FromFile("ic_star_white.png");
                    } else
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
                return Episode.is_favorite ? "favorite status favorited": "favorite status not favorited";
            }
            set { throw new Exception("You cannot set this directly"); }
        }

        public ImageSource listenedToSource
        {
            get
            {
                if (listenedToVisible)
                {
                    return ImageSource.FromFile("ic_check_box_white_3x.png");
                }
                else
                {
                    return ImageSource.FromFile("ic_check_box_outline_blank_white_3x.png");
                }
            }
        }

        public bool hasJournalVisible
        {
            get
            {
                return Episode.has_journal;
            }
            set
            {
                Episode.has_journal = value;
                OnPropertyChanged("hasJournalVisible");
            }
        }

        void UpdateDownload(object o, DabEventArgs e)
        {
            if (Episode.id.Value == e.EpisodeId)
            {
                downloadVisible = false;
                downloadProgress = e.ProgressPercentage;
            }
        }

        void DownloadComplete(object o, DabEventArgs e)
        {
            if (Episode.id.Value == e.EpisodeId && !e.Cancelled)
            {
                downloadVisible = true;
                downloadProgress = -.01;
            }
        }

        void DownloadStarted(object o, DabEventArgs e)
        {
            if (e.EpisodeId == Episode.id.Value)
            {
                progressVisible = true;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
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
