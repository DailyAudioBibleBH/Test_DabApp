using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
    public interface IFileManagement
    {
        Task<bool> DownloadEpisodeAsync(string address, dbEpisodes episode);
        bool DeleteEpisode(string episodeId, string extension);
        bool StopDownloading();
        event EventHandler<DabEventArgs> EpisodeDownloading;
        event EventHandler<DabEventArgs> EpisodeCompleted;
        bool keepDownloading { get; set; }
        bool FileExists(string fileName);
	}

    public class FileManager : IFileManagement
    {
        private IFileManagement _manager = DependencyService.Get<IFileManagement>();
        public bool keepDownloading { get => _manager.keepDownloading; set => _manager.keepDownloading = value; }

        public event EventHandler<DabEventArgs> EpisodeDownloading
        {
            add
            {
                _manager.EpisodeDownloading += value;
            }

            remove
            {
                _manager.EpisodeDownloading -= value;
            }
        }

        public event EventHandler<DabEventArgs> EpisodeCompleted
        {
            add
            {
                _manager.EpisodeCompleted += value;
            }

            remove
            {
                _manager.EpisodeCompleted -= value;
            }
        }

        public bool DeleteEpisode(string episodeId, string extension)
        {
            return _manager.DeleteEpisode(episodeId, extension);
        }

        public Task<bool> DownloadEpisodeAsync(string address, dbEpisodes episode)
        {
            return _manager.DownloadEpisodeAsync(address, episode);
        }

        public bool FileExists(string fileName)
        {
            return _manager.FileExists(fileName);
        }

        public bool StopDownloading()
        {
            return _manager.StopDownloading();
        }
    }
}
