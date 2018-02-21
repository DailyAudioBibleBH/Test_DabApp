using System;
using System.Threading.Tasks;

namespace DABApp
{
    public interface IFileManagement
    {
        Task<bool> DownloadEpisodeAsync(string address, dbEpisodes episode);
        bool DeleteEpisode(string episodeId, string extension);
        event EventHandler<DabEventArgs> EpisodeDownloading;
        event EventHandler<DabEventArgs> EpisodeCompleted;
	}
}
