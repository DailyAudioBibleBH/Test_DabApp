using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DABApp.Droid;
using SQLite;
using HockeyApp;
using Xamarin.Forms;

[assembly: Dependency(typeof(FileManagement))]
namespace DABApp.Droid
{
	public class FileManagement: IFileManagement
	{
        public event EventHandler<DabEventArgs> EpisodeDownloading;
        public event EventHandler<DabEventArgs> EpisodeCompleted;
        dbEpisodes _episode;
        double progress = -.01;
        WebClient client;
        public bool keepDownloading { get; set; } = true;

        public FileManagement()
		{
		}

		public bool DeleteEpisode(string episodeId, string extension)
		{
			try
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var fileName = Path.Combine(doc, $"{episodeId}.{extension}");
				File.Delete(fileName);
				return true;
			}
			catch (Exception e)
			{
				return false;
			}
		}

        public bool StopDownloading()
        {
            try
            {
                if (client != null)
                {
                    if (client.IsBusy)
                    {
                        client.CancelAsync();
                        keepDownloading = false;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<bool> DownloadEpisodeAsync(string address, dbEpisodes episode)
        {
            try
            {
                if (keepDownloading)
                {
                    _episode = episode;
                    var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var ext = address.Split('.').Last();
                    var fileName = Path.Combine(doc, $"{episode.id.Value.ToString()}.{ext}");
                    //if (!File.Exists(fileName)) {
                    //	File.Create(fileName);
                    //}
                    client = new WebClient();
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    await client.DownloadFileTaskAsync(address, fileName);
                    return true;
                }
                else return false;
            }
            catch (Exception e)
            {
                client.CancelAsync();
                return false;
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var b = e.Cancelled || e.Error != null;
            var a = new DabEventArgs(_episode.id.Value, -1, b);
            progress = -.01;
            Debug.WriteLine($"Download completed for {_episode.id.Value}");
            EpisodeCompleted?.Invoke(sender, a);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double pp = (((double)e.ProgressPercentage) / 100.00);
            if (pp > progress + .1 && pp < 1)
            {
                progress = pp;
                var a = new DabEventArgs(_episode.id.Value, pp);
                Debug.WriteLine($"Download Progress: {pp}");
                EpisodeDownloading?.Invoke(sender, a);
            }
        }
    }
}
