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
    public class FileManagement : IFileManagement
    {
        public event EventHandler<DabEventArgs> EpisodeDownloading;
        public event EventHandler<DabEventArgs> EpisodeCompleted;
        public static event EventHandler<DabEventArgs> DoneDownloading;
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
                    var fileName = Path.Combine(doc, $"{episode.id.Value.ToString()}.{episode.File_extension}");
                    _episode = episode;

                    client = new WebClient();
                    WebRequest request = HttpWebRequest.Create(address);
                    request.Method = "HEAD";
                    using (WebResponse response = request.GetResponse())
                    {
                        FileSize = response.ContentLength;
                    }
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    await client.DownloadFileTaskAsync(address, fileName);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                try
                {
                    client.CancelAsync();
                }
                catch (Exception e2)
                {
                    //Do nothing
                }
                return false;
            }
        }

        public bool DeleteEpisode(string episodeId, string extension)
        {
            try
            { 
                var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var fileName = Path.Combine(doc, $"{episodeId}.{extension}");
                if (File.Exists(fileName))
                { 
                Debug.WriteLine($"Deleted episode {episodeId}");
                File.Delete(fileName);
                } else
                {
                    Debug.WriteLine($"Ignored deletion of episode {episodeId}");
                }
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
                //Do nothing
                return false;
            }
        }

        public bool FileExists(string fileName)
        {
            try
            {
                var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string FileName = Path.Combine(doc, fileName);
                return File.Exists(FileName);
            }
            catch (Exception)
            {
                //Do nothing
                return false;
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                var a = new DabEventArgs(_episode.id.Value, -1, e.Cancelled);
                progress = -.01;
                EpisodeCompleted?.Invoke(sender, a);
                DoneDownloading?.Invoke(sender, a);
            }
            catch (Exception)
            {
                //Do nothing
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {

            try
            {
                double pp = ((double)e.BytesReceived) / FileSize;
                //double pp = e.ProgressPercentage / 100.0;
                if (pp > progress + .1)
                {
                    progress = pp;
                    var a = new DabEventArgs(_episode.id.Value, pp);
                    EpisodeDownloading?.Invoke(sender, a);
                }
            }
            catch (Exception)
            {
                //Do nothing
            }
        }
    }
}
