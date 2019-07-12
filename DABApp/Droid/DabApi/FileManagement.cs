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
    public class FileManagement : IFileManagement
    {
        public event EventHandler<DabEventArgs> EpisodeDownloading;
        public event EventHandler<DabEventArgs> EpisodeCompleted;
        public event EventHandler<DabEventArgs> ChangeVisualDownload;
        dbEpisodes _episode;
        double progress = -.01;
        WebClient client;
        public bool keepDownloading { get; set; } = true;
        long FileSize;

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
                    var fileName = Path.Combine(doc, $"{episode.id.Value.ToString()}.{episode.File_extension}");
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
                else return false;
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
                var c = new DabEventArgs(_episode.id.Value, 1, false); //new
                var b = e.Cancelled || e.Error != null;
                var a = new DabEventArgs(_episode.id.Value, -1, b);
                progress = -.01;
                Debug.WriteLine($"Download completed for {_episode.id.Value}");
                ChangeVisualDownload?.Invoke(sender, c); //new 
                //Change property of blue cloud with event handling 
                //Need to call HandleDownloadVisibleChanged here
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
                if (pp > progress + .1 && pp < 1)
                {
                    progress = pp;
                    var a = new DabEventArgs(_episode.id.Value, pp);
                    Debug.WriteLine($"Download Progress: {pp}");
                    EpisodeDownloading?.Invoke(sender, a);
                    //Code exits here during download when following breakpoints
                }

            }
            catch (Exception)
            {
                //Do nothing
            }
        }
    }
}
