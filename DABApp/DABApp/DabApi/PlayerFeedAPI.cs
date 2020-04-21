using System;
//using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using SQLite;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Diagnostics;
using Plugin.Connectivity;
using System.Linq;
using DABApp.DabSockets;

namespace DABApp
{
    public class PlayerFeedAPI
    {

        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        static bool DownloadIsRunning = false;
        static bool CleanupIsRunning = false;
        static bool ResumeNotSet = true;
        public static event EventHandler<DabEventArgs> MakeProgressVisible;

        public static IEnumerable<dbEpisodes> GetEpisodeList(Resource resource)
        {
            //GetEpisodes(resource);
            return adb.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate).ToListAsync().Result;
                
        }

        //grab episodes by channel
        public static async Task<string> GetEpisodes(List<DabGraphQlEpisode> episodesList, dbChannels channel)
        {
            try
            {
                var fromDate = DateTime.Now.Month == 1 ? $"{(DateTime.Now.Year - 1).ToString()}-12-01" : $"{DateTime.Now.Year}-01-01";

                List<DabGraphQlEpisode> currentEpisodes = new List<DabGraphQlEpisode>();

                foreach (var item in episodesList)
                {
                    if (item.date >= Convert.ToDateTime(fromDate) && item.channelId == channel.channelId)
                    {
                        currentEpisodes.Add(item);
                    }
                    else
                    {
                        currentEpisodes.Remove(item);
                    }
                }

                //var EpisodeMeta = db.Table<dbUserEpisodeMeta>().ToList();
                List<int> episodesToGetActionsFor = new List<int>();
                if (currentEpisodes == null)
                {
                    return "Server Error";
                }
                var code = channel.title == "Daily Audio Bible" ? "dab" : channel.title.ToLower();
                var existingEpisodes = adb.Table<dbEpisodes>().Where(x => x.channel_code == code).ToListAsync().Result;
                var existingEpisodeIds = existingEpisodes.Select(x => x.id).ToList();
                var newEpisodeIds = currentEpisodes.Select(x => x.episodeId);
                var start = DateTime.Now;
                foreach (var e in currentEpisodes)
                {
                    if (!existingEpisodeIds.Contains(e.episodeId))
                    {
                        //build out rest of episodes object since we don't get this from websocket
                        dbEpisodes episode = new dbEpisodes(e);
                        episode.channel_title = channel.title;
                        //episode.channel_description = channel.;
                        episode.channel_code = channel.title == "Daily Audio Bible" ? "dab" : channel.title.ToLower();
                        episode.PubMonth = getMonth(e.date);
                        episode.PubDay = e.date.Day;

                        Device.InvokeOnMainThreadAsync(async () =>
                        {
                            await adb.InsertOrReplaceAsync(episode);
                        });
                    }
                }

                Debug.WriteLine($"Finished with GetEpisodes() {(DateTime.Now - start).TotalMilliseconds}");
                return "OK";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception called in Getting episodes: {ex.Message}");
                return ex.Message;
            }
        }

        public static string getMonth(DateTime e)
        {
            switch (e.Month)
            {
                case (1):
                    return "Jan";
                case (2):
                    return "Feb";
                case (3):
                    return "Mar";
                case (4):
                    return "Apr";
                case (5):
                    return "May";
                case (6):
                    return "Jun";
                case (7):
                    return "Jul";
                case (8):
                    return "Aug";
                case (9):
                    return "Sep";
                case (10):
                    return "Oct";
                case (11):
                    return "Nov";
                case (12):
                    return "Dec";
                default:
                    return "";
            }
        }

        public static async Task<dbEpisodes> GetMostRecentEpisode(Resource resource)
        {
            var episode = await adb.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate).FirstOrDefaultAsync();
            return episode;
        }

        public static dbEpisodes GetEpisode(int id)
        {
            return adb.Table<dbEpisodes>().Where(x => x.id == id).FirstAsync().Result;
        }

        public static void CheckOfflineEpisodeSettings()
        {
            var offlineSettings = adb.Table<dbSettings>().Where(x => x.Key == "OfflineEpisodes").FirstOrDefaultAsync().Result;
            if (offlineSettings == null)
            {
                offlineSettings = new dbSettings();
                offlineSettings.Key = "OfflineEpisodes";
                OfflineEpisodeSettings settings = new OfflineEpisodeSettings();
                settings.Duration = "One Day";
                settings.DeleteAfterListening = false;
                var jsonSettings = JsonConvert.SerializeObject(settings);
                offlineSettings.Value = jsonSettings;
                adb.InsertAsync(offlineSettings);
                OfflineEpisodeSettings.Instance = settings;
            }
            else
            {
                var current = offlineSettings.Value;
                OfflineEpisodeSettings.Instance = JsonConvert.DeserializeObject<OfflineEpisodeSettings>(current);
            }
        }

        public static void UpdateOfflineEpisodeSettings()
        {
            var offlineSettings = adb.Table<dbSettings>().Where(x => x.Key == "OfflineEpisodes").FirstAsync().Result;
            offlineSettings.Value = JsonConvert.SerializeObject(OfflineEpisodeSettings.Instance);
            adb.UpdateAsync(offlineSettings);
        }

        public static async Task<bool> DownloadEpisodes()
        {

            bool RunAgain = false;
            var OfflineChannels = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Where(x => x.availableOffline == true);
            DateTime cutoffTime = new DateTime();
            switch (OfflineEpisodeSettings.Instance.Duration)
            {
                case "One Day":
                    cutoffTime = DateTime.Now.AddDays(-1);
                    break;
                case "Two Days":
                    cutoffTime = DateTime.Now.AddDays(-2);
                    break;
                case "Three Days":
                    cutoffTime = DateTime.Now.AddDays(-3);
                    break;
                case "One Week":
                    if (GlobalResources.TestMode)
                    {
                        cutoffTime = DateTime.Now.AddDays(-21);
                    }
                    else
                    {
                        cutoffTime = DateTime.Now.AddDays(-7);
                    }
                    break;
                default:
                    cutoffTime = DateTime.Now.AddDays(-7);
                    break;
                    //case "One Month":
                    //	cutoffTime = DateTime.Now.AddMonths(-1);
                    //	break;
            }
            //Get episodes to download
            var episodesToShowDownload = new List<dbEpisodes>();
            var EpisodesToDownload = from channel in OfflineChannels
                                     join episode in adb.Table<dbEpisodes>().ToListAsync().Result on channel.title equals episode.channel_title
                                     where !episode.is_downloaded //not downloaded
                                                           && episode.PubDate > cutoffTime //new enough to be downloaded
                                                           && (!OfflineEpisodeSettings.Instance.DeleteAfterListening || episode.UserData.IsListenedTo != true) //not listened to or system not set to delete listened to episodes
                                     orderby episode.PubDate descending
                                     select episode;
            episodesToShowDownload = EpisodesToDownload.ToList();
            foreach (var episode in episodesToShowDownload)
            {
                try
                {
                    if (!episode.progressVisible)
                    {
                        episode.progressVisible = true;
                        await adb.UpdateAsync(episode);
                    }
                    MakeProgressVisible?.Invoke(episode, new DabEventArgs(episode.id.Value, -1, false));
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error while setting episodes to progress visible. Error: {e.Message}");
                }
            }
            if (!DownloadIsRunning)
            {
                FileManager fm = new FileManager();
                fm.keepDownloading = true;
                DownloadIsRunning = true;
                var episodesToDownload = new List<dbEpisodes>();
                episodesToDownload = episodesToShowDownload;
                int ix = 0;
                //List<dbEpisodes> episodesToUpdate = new List<dbEpisodes>();
                foreach (var episode in episodesToDownload)
                {
                    try
                    {
                        ix++;
                        Debug.WriteLine("Starting to download episode {0} ({1}/{2} - {3})...", episode.id, ix, episodesToShowDownload.Count(), episode.url);
                        if (await fm.DownloadEpisodeAsync(episode.url, episode))
                        {
                            Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update"); });
                            Debug.WriteLine("Finished downloading episode {0} ({1})...", episode.id, episode.url);
                            episode.is_downloaded = true;
                            await adb.UpdateAsync(episode);
                        }
                        else throw new Exception("Error called by the DownloadEpisodeAsync method of the IFileManagement dependency service.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error while downloading episode. Downloads will continue. Error:  {ex.Message}");

                        CrossConnectivity.Current.ConnectivityChanged += ResumeDownload;
                        ResumeNotSet = false;
                    }
                }
                Debug.WriteLine("Downloads complete!");
                DownloadIsRunning = false;
                if (RunAgain)
                {
                    await DownloadEpisodes();
                }

                //Cleanup episodes
                PlayerFeedAPI.CleanUpEpisodes();

                return true;
            }
            else
            {
                //download is already running
                RunAgain = true;
                return true;
            }
        }

        static async void ResumeDownload(object o, Plugin.Connectivity.Abstractions.ConnectivityChangedEventArgs e)
        {
            if (e.IsConnected)
            {
                await DownloadEpisodes();
                CrossConnectivity.Current.ConnectivityChanged -= ResumeDownload;
                ResumeNotSet = true;
            }
        }

        public static async Task DeleteChannelEpisodes(Resource resource)
        {
            try
            {
                FileManager fm = new FileManager();
                fm.StopDownloading();
                DownloadIsRunning = false;
                var Episodes = adb.Table<dbEpisodes>().Where(x => x.channel_title == resource.title && (x.is_downloaded || x.progressVisible)).ToListAsync().Result;
                foreach (var episode in Episodes)
                {
                    var ext = episode.url.Split('.').Last();
                    if (fm.DeleteEpisode(episode.id.ToString(), ext))
                    {
                        episode.is_downloaded = false;
                        episode.progressVisible = false;
                        //if (Device.Idiom == TargetIdiom.Tablet && Device.RuntimePlatform == Device.Android)
                        //{
                        //    db.Update(episode);
                        //}
                        //else
                        //{
                        await adb.UpdateAsync(episode);
                        //}
                        Debug.WriteLine($"Episode: {episode.title} deleted");
                        //if (Device.Idiom == TargetIdiom.Tablet)
                        //{
                        //    Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update"); });
                        //}
                    }
                    else
                    {
                        throw new Exception($"Error deleting episode for channel: {resource.title}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception caught in DeleteChannelEpisodes: {e.Message}");
            }
            Debug.WriteLine($"Episodes for {resource.title} Deleted");
        }

        public static async Task UpdateEpisodeProperty(int episodeId, bool? isListened, bool? isFavorite, bool? hasJournal, int? playerPosition, bool RaiseEpisodeDataChanged = true)
        {
            try
            {
                //find the epissode user data
                if (GuestStatus.Current.IsGuestLogin)
                {
                    bool answer = await Application.Current.MainPage.DisplayAlert("Login Required", "You must be logged in to use this feature. Your settings will be saved locally, but may be lost when your app is updated.", "Log In", "Ignore");

                    if (answer == true)
                    {
                        GlobalResources.LogoffAndResetApp();
                    }
                }
                else
                {
                    var userName = GlobalResources.GetUserEmail();
                    dbEpisodeUserData data = adb.Table<dbEpisodeUserData>().Where(x => x.EpisodeId == episodeId && x.UserName == userName).FirstOrDefaultAsync().Result;
                    if (data == null)
                    {
                        data = new dbEpisodeUserData();
                        data.EpisodeId = episodeId;
                        data.UserName = userName;
                    }
                    data.HasJournal = (hasJournal == null) ? false : hasJournal.Value;
                    data.IsFavorite = (isFavorite == null) ? false : isFavorite.Value;
                    data.IsListenedTo = (isListened == null) ? false : isListened.Value;
                    if (playerPosition.HasValue)
                    {
                        if (GlobalResources.CurrentEpisodeId == episodeId)
                        {
                            if (!GlobalResources.playerPodcast.IsPlaying)
                            {
                                //update the active player (only if it is paused)
                                data.CurrentPosition = playerPosition.Value;
                                //TODO: Need to update this?
                                //episode.remaining_time = (episode.Duration - episode.UserData.CurrentPosition).ToString();
                                GlobalResources.playerPodcast.Seek(data.CurrentPosition);
                            }
                            else
                            {
                                Debug.WriteLine("Skipping seek to new position since episode is playing...");
                            }
                        }
                        else
                        {
                            data.CurrentPosition = playerPosition.Value;
                        }

                    }
                    adb.InsertOrReplaceAsync(data);
                    Debug.WriteLine($"Added episode {episodeId}/{userName} to user episode for later use...");

                    //Notify listening pages that episode data has changed
                    if (RaiseEpisodeDataChanged)
                    {
                        MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                        //MessagingCenter.Send<string>("Update", "Update");
                    }
                }
            }
            catch (Exception e)
            {
                //Getting Locked exception on android 
                Debug.WriteLine($"Exception in PlayerFeedAPI.UpdateEpisodeProperty(): {e.Message}");
                DabData.ResetDatabases();
                adb = DabData.AsyncDatabase;
            }
        }

        public static void CleanUpEpisodes()
        {
            if (!CleanupIsRunning)
            {
                CleanupIsRunning = true;
                DateTime cutoffTime = new DateTime();
                switch (OfflineEpisodeSettings.Instance.Duration)
                {
                    case "One Day":
                        cutoffTime = DateTime.Now.AddDays(-1);
                        break;
                    case "Two Days":
                        cutoffTime = DateTime.Now.AddDays(-2);
                        break;
                    case "Three Days":
                        cutoffTime = DateTime.Now.AddDays(-3);
                        break;
                    case "One Week":
                        //When in staging, use 3 weeks instead of 1 for testing
                        if (GlobalResources.TestMode)
                        {
                            cutoffTime = DateTime.Now.AddDays(-21);
                        } else
                        {
                            cutoffTime = DateTime.Now.AddDays(-7);
                        }
                        break;
                        //case "One Month":
                        //	cutoffTime = DateTime.Now.AddMonths(-1);
                        //	break;
                }
                Debug.WriteLine("Cleaning up episodes...");
                List<dbEpisodes> episodesToDelete = new List<dbEpisodes>();
                if (OfflineEpisodeSettings.Instance.DeleteAfterListening)
                {
                    var eps = from x in adb.Table<dbEpisodes>()
                              where (x.is_downloaded || x.progressVisible)  //downloaded episodes
                              select x;
                    //simplified query and added foreach iteration since query was giving null object reference on x.userdata.islistenedto
                    foreach (var item in eps.ToListAsync().Result)
                    {
                        if (item.UserData.IsListenedTo == true || item.PubDate < cutoffTime)
                            episodesToDelete.Add(item);
                    }
                }
                else
                {
                    var eps = from x in adb.Table<dbEpisodes>()
                              where (x.is_downloaded || x.progressVisible) && x.PubDate < cutoffTime
                              select x;
                    episodesToDelete = eps.ToListAsync().Result;
                }
                Debug.WriteLine("Cleaning up {0} episodes...", episodesToDelete.Count());
                foreach (var episode in episodesToDelete)
                {
                    Debug.WriteLine("Cleaning up episode {0} ({1})...", episode.id, episode.url);
                    try
                    {
                        FileManager fm = new FileManager();
                        if (fm.DeleteEpisode(episode.id.ToString(), episode.File_extension))
                        {
                            Debug.WriteLine("Episode {0} deleted.", episode.id, episode.url);
                            episode.is_downloaded = false;
                            episode.progressVisible = false;
                            adb.UpdateAsync(episode);
                                Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update"); });
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to delete episode {0}", episode.id);
                    }
                }
                Debug.WriteLine("Cleanup complete.");
                CleanupIsRunning = false;
            }
            else
            {
                Debug.WriteLine("Cleanup already running...");
                //cleanup already running

            }
        }

        public static async Task UpdateStopTime(int CurrentEpisodeId, double NewStopTime, double NewRemainingTime)
        {
            try
            {
                var episode = adb.Table<dbEpisodes>().Where(x => x.id == CurrentEpisodeId).FirstAsync().Result;
                episode.UserData.CurrentPosition = NewStopTime;
                Debug.WriteLine($"New Stop  Time: {NewStopTime / 60}");
                episode.remaining_time = NewRemainingTime.ToString(); //TODO was a string - did making this a double break it?
                await adb.UpdateAsync(episode);
                await adb.UpdateAsync(episode.UserData);
                //if (Device.Idiom == TargetIdiom.Tablet)
                //{
                Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update"); });
                //}
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception thrown in PlayerFeedAPI.UpdateStopTime(): {e.Message}");
                //DabData.ResetDatabases();
                //db = DabData.database;
                //adb = DabData.AsyncDatabase;
            }
        }

        public static async Task<Reading> GetReading(string ReadLink)
        {
            try
            {
                if (String.IsNullOrEmpty(ReadLink))
                {
                    throw new Exception("Error getting reading: No Read Link");
                }
                HttpClient client = new HttpClient();
                var result = await client.GetAsync(ReadLink);
                var JsonOut = await result.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeObject<Reading>(JsonOut);
                if (content.message != null)
                {
                    //Return a fake reading.
                    return new Reading()
                    {
                        excerpts = new List<String>(),
                        id = -1,
                        IsAlt = true,
                        link = "",
                        message = $"Error getting reading: {content.message}",
                        text = $"Error getting reading: {content.message}",
                        title = "Error getting reading"
                    };
                }
                if (content.link != ReadLink)
                {
                    content.IsAlt = true;
                }
                return content;
            }
            catch (HttpRequestException re)
            {
                var reading = new Reading();
                reading.title = "An Http Request Exception has been called.  This may be due to problems with your network.  Please check your internet connection and try again.";
                return reading;
            }
            catch (Exception e)
            {
                var reading = new Reading();
                if (e.InnerException != null)
                {

                    if (e.InnerException.GetType() == typeof(HttpRequestException))
                    {
                        reading.title = "Due to copyright issues we cannot display read along text without an internet connection.  Please check your internet connection and try again.";
                    }
                    else reading.title = e.InnerException.Message;
                }
                else
                {
                    if (String.IsNullOrEmpty(ReadLink))
                    {
                        reading.title = "We're sorry, but text to read-along with this episode has not been set up.";
                    }
                    else
                    {
                        reading.title = "We're sorry but an error was encountered while loading the text that goes with this episode.";
                    }
                }
                return reading;
            }
        }

        public static async Task<string> PostDonationAccessToken(string campaignId = "1")
        {
            try
            {
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                dbSettings CreationSettings = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
                dbSettings AvatarSettings = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
                var token = new APIToken
                {
                    value = TokenSettings.Value,
                    expires = CreationSettings.Value,
                    user_email = EmailSettings.Value,
                    user_first_name = FirstNameSettings.Value,
                    user_last_name = LastNameSettings.Value,
                    user_avatar = AvatarSettings.Value
                };
                HttpClient client = new HttpClient();
                var tres = await client.GetAsync($"{GlobalResources.GiveUrl}donation/request_access");
                var tok = await tres.Content.ReadAsStringAsync();
                var en = JsonConvert.DeserializeObject<GetTokenContainer>(tok);
                var send = new DonationTokenContainer
                {
                    token = token,
                    csrf_dab_token = en.csrf.token_value,
                    campaign_id = campaignId
                };
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                string JsonIn = JsonConvert.SerializeObject(send);
                HttpContent content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = await client.PostAsync($"{GlobalResources.GiveUrl}donation/request_access", content);
                var JsonOut = await result.Content.ReadAsStringAsync();
                if (!JsonOut.Contains("url"))
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(JsonOut);
                    throw new Exception(error.message);
                }
                var cont = JsonConvert.DeserializeObject<RequestedUrl>(JsonOut);
                return cont.url;
            }
            catch (Exception e)
            {
                return $"Error caught in PlayerFeedAPI.PostDonationAccessToken(): {e.Message}";
            }
        }
    }
}
