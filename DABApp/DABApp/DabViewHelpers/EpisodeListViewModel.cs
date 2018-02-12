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
    public class EpisodeListViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<dbEpisodes> _episodes;

        public EpisodeListViewModel()
        {
            _episodes = new ObservableCollection<dbEpisodes>();
            DependencyService.Get<IFileManagement>().EpisodeDownloading += UpdateDownload;
        }

        private void UpdateDownload(object sender, DabEventArgs e)
        {
            var ep = _episodes.FirstOrDefault(x => x.id.Value == e.EpisodeId);
            if (ep != null)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ep.downloadProgress = e.ProgressPercentage;
                    PropertyChanged(this, new PropertyChangedEventArgs("episodes"));
                });
            }
        }

        public ObservableCollection<dbEpisodes> episodes
        {
            get {
                return _episodes;
            }
            set {
                _episodes = value;
                PropertyChanged(this, new PropertyChangedEventArgs("episodes"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
