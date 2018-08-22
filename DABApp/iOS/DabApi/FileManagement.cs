using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using DABApp.iOS;
using System.Linq;
using System.Diagnostics;

[assembly: Dependency(typeof(FileManagement))]
namespace DABApp.iOS
{
	public class FileManagement: IFileManagement
	{
        public event EventHandler<DabEventArgs> EpisodeDownloading;
        public event EventHandler<DabEventArgs> EpisodeCompleted;
        dbEpisodes _episode;
        double progress = -.01;
        WebClient client;
        public bool keepDownloading { get; set; } = true;
        long FileSize;

        public async Task<bool> DownloadEpisodeAsync(string address, dbEpisodes episode)
		{
			try
			{
                if (keepDownloading)
                {
                    var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var ext = address.Split('.').Last();
                    var fileName = Path.Combine(doc, $"{episode.id.Value.ToString()}.{ext}");
                    _episode = episode;
                    //if (!File.Exists(fileName)) {
                    //	File.Create(fileName);
                    //}
                    client = new WebClient();
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    await client.OpenReadTaskAsync(address);
                    FileSize = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                    await client.DownloadFileTaskAsync(address, fileName);
                    return true;
                }
                return false;
			}
			catch (Exception e)
			{
				return false;
			}
		}

		public bool DeleteEpisode(string episodeId, string extension) {
			try
			{
                var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var fileName = Path.Combine(doc, $"{episodeId}.{extension}");
				Debug.WriteLine($"Deleted episode {episodeId}");
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

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var a = new DabEventArgs(_episode.id.Value, -1, e.Cancelled);
            progress = -.01;
            EpisodeCompleted?.Invoke(sender, a);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double pp = (double)e.BytesReceived / FileSize;
            if (pp > progress + .1)
            {
                progress = pp;
                var a = new DabEventArgs(_episode.id.Value, pp);
                EpisodeDownloading?.Invoke(sender, a);
            }
        }
    }
}
