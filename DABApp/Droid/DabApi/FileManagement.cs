using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DABApp.Droid;
using SQLite;
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

        public async Task<bool> DownloadEpisodeAsync(string address, dbEpisodes episode)
        {
            try
            {
                _episode = episode;
                var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var ext = address.Split('.').Last();
                var fileName = Path.Combine(doc, $"{episode.id.Value.ToString()}.{ext}");
                //if (!File.Exists(fileName)) {
                //	File.Create(fileName);
                //}
                WebClient client = new WebClient();
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadFileCompleted += Client_DownloadFileCompleted;
                await client.DownloadFileTaskAsync(address, fileName);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                var a = new DabEventArgs(_episode.id.Value, -1);
                progress = -.01;
                EpisodeCompleted?.Invoke(sender, a);
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double pp = (((double)e.ProgressPercentage) / 100.00);
            if (pp > progress + .1)
            {
                progress = pp;
                var a = new DabEventArgs(_episode.id.Value, pp);
                EpisodeDownloading?.Invoke(sender, a);
            }
        }
    }
}
