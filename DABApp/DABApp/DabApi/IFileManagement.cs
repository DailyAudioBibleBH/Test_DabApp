using System;
using System.Threading.Tasks;

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
	}
}
