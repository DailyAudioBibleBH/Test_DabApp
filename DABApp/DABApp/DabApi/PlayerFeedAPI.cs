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
using DABApp.Service;

namespace DABApp
{
    public class PlayerFeedAPI
    {

        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        public static bool DownloadIsRunning = false;
        static bool CleanupIsRunning = false;
        static bool ResumeNotSet = true;
        public static event EventHandler<DabEventArgs> MakeProgressVisible;

        public async static Task<IEnumerable<dbEpisodes>> GetEpisodeList(Resource resource)
        {
            dbChannels channel = await adb.Table<dbChannels>().Where(x => x.channelId == resource.id).FirstOrDefaultAsync();
            List<dbEpisodes> episodesTable = adb.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate).ToListAsync().Result;
            DateTime beginEpisodeDate = GlobalResources.DabMinDate;

            //Year end rollover process for guest
            if (GuestStatus.Current.IsGuestLogin)
            {
                DateTime startDate = new DateTime(DateTime.Now.Year, channel.rolloverMonth, channel.rolloverDay);
                DateTime todaysDate = DateTime.Now.Date;
                int bufferLength = channel.bufferLength;
                int bufferPeriod = channel.bufferPeriod;
                DateTime startRolloverDate = todaysDate.AddDays(-bufferLength);
                DateTime stopImpactDate = startDate.AddDays(bufferPeriod);
                
                //if today is within buffer period
                if (todaysDate.CompareTo(startDate) >= 0 && todaysDate.CompareTo(stopImpactDate) <= 0)
                {
                    return episodesTable.Where(x => x.PubDate.CompareTo(startRolloverDate) >= 0).OrderByDescending(x => x.PubDate).ToList();
                }
                else
                {
                    return episodesTable.Where(x => x.PubDate.CompareTo(startDate) >= 0).OrderByDescending(x => x.PubDate).ToList();
                }
            }

            return episodesTable.Where(x => x.PubDate.CompareTo(beginEpisodeDate) >= 0).OrderByDescending(x => x.PubDate).ToList();
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
            string offlineSettingsValue = dbSettings.GetSetting("OfflineEpisodes", "");
            if (offlineSettingsValue == "")
            {
                OfflineEpisodeSettings settings = new OfflineEpisodeSettings();
                settings.Duration = "One Day";
                settings.DeleteAfterListening = false;
                var jsonSettings = JsonConvert.SerializeObject(settings);
                dbSettings.StoreSetting("OfflineEpisodes", jsonSettings);
                OfflineEpisodeSettings.Instance = settings;
            }
            else
            {
                var current = offlineSettingsValue;
                OfflineEpisodeSettings.Instance = JsonConvert.DeserializeObject<OfflineEpisodeSettings>(current);
            }
        }

        public static void UpdateOfflineEpisodeSettings()
        {
            string offlineSettingsValue = JsonConvert.SerializeObject(OfflineEpisodeSettings.Instance);
            dbSettings.StoreSetting("OfflineEpisodes", offlineSettingsValue);
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
                            Debug.WriteLine("Finished downloading episode {0} ({1})...", episode.id, episode.url);
                            episode.is_downloaded = true;
                            await adb.UpdateAsync(episode);
                            Service.DabServiceEvents.EpisodeUserDataChanged(); //notify listeners that episode has changed
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

        public static async Task UpdateEpisodeUserData(int episodeId, bool? isListened, bool? isFavorite, bool? hasJournal, int? playerPosition, bool RaiseChangedEvent = false)
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
                    //find the user episode data (ued) in question
                    var userName = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Email;
                    dbEpisodeUserData data = adb.Table<dbEpisodeUserData>().Where(x => x.EpisodeId == episodeId && x.UserName == userName).FirstOrDefaultAsync().Result;

                    //add new ued if needed 
                    if (data == null)
                    {
                        data = new dbEpisodeUserData();
                        data.EpisodeId = episodeId;
                        data.UserName = userName;
                    }
                    //set journal if needed
                    if (hasJournal.HasValue)
                    { 
                        data.HasJournal = hasJournal.Value;
                    }
                    //set favorite if needed
                    if (isFavorite.HasValue)
                    {
                        data.IsFavorite = isFavorite.Value;
                    }
                    //set listened if needed
                    if (isListened.HasValue)
                    {
                        data.IsListenedTo = isListened.Value;
                    }
                    //set position if needed
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
                    await adb.InsertOrReplaceAsync(data);
                    Debug.WriteLine($"Saved episode {episodeId}/{userName} meta data: {JsonConvert.SerializeObject(data)}");

                    //Notify listening pages that episode data has changed, if requested
                    if (RaiseChangedEvent)
                    {
                        DabServiceEvents.EpisodeUserDataChanged();
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
                            var x = adb.UpdateAsync(episode).Result;
                            Service.DabServiceEvents.EpisodeUserDataChanged();
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
                Service.DabServiceEvents.EpisodeUserDataChanged();
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
                var tokenValue = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Token;
                var creation = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.TokenCreation;
                var email = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Email;
                var firstName = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.FirstName;
                var lastName = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.LastName;
                var avatar = GlobalResources.UserAvatar;
                
                var token = new APIToken
                {
                    value = tokenValue,
                    expires = creation.ToString(),
                    user_email = email,
                    user_first_name = firstName,
                    user_last_name = lastName,
                    user_avatar = avatar
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
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenValue);
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
