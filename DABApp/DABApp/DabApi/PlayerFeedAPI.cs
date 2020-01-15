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
using DABApp.WebSocketHelper;
using DABApp.DabSockets;

namespace DABApp
{
    public class PlayerFeedAPI
    {
        static SQLiteConnection db = DabData.database;
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        static bool DownloadIsRunning = false;
        static bool CleanupIsRunning = false;
        static bool ResumeNotSet = true;
        public static event EventHandler<DabEventArgs> MakeProgressVisible;

        public static IEnumerable<dbEpisodes> GetEpisodeList(Resource resource)
        {
            //GetEpisodes(resource);
            return db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate);
        }

        public static async Task<string> GetEpisodes(Resource resource)
        {
            try
            {
                var client = new HttpClient();
                var fromDate = DateTime.Now.Month == 1 ? $"{(DateTime.Now.Year - 1).ToString()}-12-01" : $"{DateTime.Now.Year}-01-01";
                var result = /*GlobalResources.TestMode ? await client.GetAsync(resource.feedUrl) :*/ await client.GetAsync($"{resource.feedUrl}?fromdate={fromDate}&&todate={DateTime.Now.Year}-12-31");
                string jsonOut = await result.Content.ReadAsStringAsync();
                var Episodes = JsonConvert.DeserializeObject<List<dbEpisodes>>(jsonOut);
                //var EpisodeMeta = db.Table<dbUserEpisodeMeta>().ToList();
                List<int> episodesToGetActionsFor = new List<int>();
                if (Episodes == null)
                {
                    return "Server Error";
                }
                var code = resource.title == "Daily Audio Bible" ? "dab" : resource.title.ToLower();
                var existingEpisodes = db.Table<dbEpisodes>().Where(x => x.channel_code == code).ToList();
                var existingEpisodeIds = existingEpisodes.Select(x => x.id).ToList();
                var newEpisodeIds = Episodes.Select(x => x.id);
                var start = DateTime.Now;
                foreach (var e in Episodes)
                {
                    if (!existingEpisodeIds.Contains(e.id))
                    {
                        ////get user-episode meta data from the database if we have it
                        //dbUserEpisodeMeta meta = EpisodeMeta.SingleOrDefault(x => x.EpisodeId == e.id);
                        //if (meta != null)
                        //{
                        //    e.stop_time = (meta.CurrentPosition == null) ? 0 : meta.CurrentPosition.Value;
                        //    e.is_favorite = (meta.IsFavorite == null) ? false : meta.IsFavorite.Value;
                        //    e.has_journal = (meta.HasJournal == null) ? false : meta.HasJournal.Value;
                        //    e.is_listened_to = (meta.IsListenedTo == null) ? false : meta.IsListenedTo.Value;
                        //    Debug.WriteLine($"Loaded episode user meta for {e.id}");
                        //} else
                        //{
                        //    Debug.WriteLine($"No user meta for {e.id}");
                        //}

                        //adding an episode to the database
                        await adb.InsertOrReplaceAsync(e);

                        ////add episode to list of episodes to query actions from
                        //episodesToGetActionsFor.Add(e.id.Value);
                    }
                }

                ////send off request to get new episode data
                ////Send last action query to the websocket
                //int c = episodesToGetActionsFor.Count();
                //if (c > 0)
                //{
                //    Variables variables = new Variables();
                //    Debug.WriteLine($"Getting actions for {c} new episodes...");
                //    var newEpisodeQuery = "query{ actions(episodeIds: " + JsonConvert.SerializeObject(episodesToGetActionsFor) + ") { edges { id episodeId userId favorite listen position entryDate updatedAt createdAt } } } ";
                //    var newEpisodePayload = new WebSocketHelper.Payload(newEpisodeQuery, variables);
                //    var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", newEpisodePayload));
                //    DabSyncService.Instance.Send(JsonIn);
                //}


                Debug.WriteLine($"Starting deletion {(DateTime.Now - start).TotalMilliseconds}");
                foreach (var old in existingEpisodes)
                {
                    if (!newEpisodeIds.Contains(old.id))
                    {
                        await adb.DeleteAsync(old);
                    }
                }
                Debug.WriteLine($"Finished inserting and deleting episodes {(DateTime.Now - start).TotalMilliseconds}");
                if (resource.availableOffline && Device.Idiom == TargetIdiom.Tablet)
                {
                    Task.Run(async () => { await DownloadEpisodes(); });
                }
                //var b = await AuthenticationAPI.GetMemberData();//This slows down everything
                //if (!b)
                //{
                //	db = DabData.database;
                //	adb = DabData.AsyncDatabase;
                //}
                Debug.WriteLine($"Finished with GetEpisodes() {(DateTime.Now - start).TotalMilliseconds}");
                return "OK";
                //else {
                //	throw new Exception(); 
                //}
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception called in Getting episodes: {e.Message}");
                return e.Message;
            }
        }

        public static async Task<dbEpisodes> GetMostRecentEpisode(Resource resource)
        {
            var episode = await adb.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate).FirstOrDefaultAsync();
            return episode;
        }

        public static dbEpisodes GetEpisode(int id)
        {
            return db.Table<dbEpisodes>().Single(x => x.id == id);
        }

        public static void CheckOfflineEpisodeSettings()
        {
            var offlineSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "OfflineEpisodes");
            if (offlineSettings == null)
            {
                offlineSettings = new dbSettings();
                offlineSettings.Key = "OfflineEpisodes";
                OfflineEpisodeSettings settings = new OfflineEpisodeSettings();
                settings.Duration = "One Day";
                settings.DeleteAfterListening = false;
                var jsonSettings = JsonConvert.SerializeObject(settings);
                offlineSettings.Value = jsonSettings;
                db.Insert(offlineSettings);
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
            var offlineSettings = db.Table<dbSettings>().Single(x => x.Key == "OfflineEpisodes");
            offlineSettings.Value = JsonConvert.SerializeObject(OfflineEpisodeSettings.Instance);
            db.Update(offlineSettings);
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
                                     join episode in db.Table<dbEpisodes>() on channel.title equals episode.channel_title
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
                //if (Device.Idiom == TargetIdiom.Tablet)
                //{
                //	Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update"); });
                //}
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
                var Episodes = db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title && (x.is_downloaded || x.progressVisible)).ToList();
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

        public static async Task UpdateEpisodeProperty(int episodeId, bool? isListened, bool? IsFavorite, bool? hasJournal, int? playerPosition, bool RaiseEpisodeDataChanged = true)
        {
            try
            {
                //find the epissode
                var episode = db.Table<dbEpisodes>().SingleOrDefault(x => x.id == episodeId);
                if (episode != null) //only update episodes we have in the database
                {
                    //listened
                    if (isListened != null)
                    {
                        episode.UserData.IsListenedTo = (bool)isListened;
                    }
                    //favorite
                    if (IsFavorite.HasValue)
                    {
                        episode.UserData.IsFavorite = (bool)IsFavorite;
                    }
                    //has journal
                    if (hasJournal.HasValue)
                    {
                        episode.UserData.HasJournal = (bool)hasJournal;
                    }
                    //player position
                    if (playerPosition.HasValue)
                    {
                        if (GlobalResources.CurrentEpisodeId == episode.id)
                        {
                            if (!GlobalResources.playerPodcast.IsPlaying)
                            {
                                //update the active player (only if it is paused)
                                episode.UserData.CurrentPosition = playerPosition.Value;
                                episode.remaining_time = (episode.Duration - episode.UserData.CurrentPosition).ToString();
                                GlobalResources.playerPodcast.Seek(episode.UserData.CurrentPosition);
                            } else
                            {
                                Debug.WriteLine("Skipping seek to new position since episode is playing...");
                            }
                        }
                        //
                    }
                    //save data to the database
                    db.Update(episode);
                }
                else
                {
                    //Store the record in the episode-user-data table for later use
                    string userName = GlobalResources.GetUserEmail();
                    dbEpisodeUserData data = db.Table<dbEpisodeUserData>().SingleOrDefault(x => x.EpisodeId == episodeId && x.UserName == userName) ;
                    if (data == null)
                    {
                        data = new dbEpisodeUserData();
                        data.EpisodeId = episodeId;
                        data.UserName = userName;
                    }
                    data.CurrentPosition = playerPosition == null ? 0 : playerPosition.Value;
                    data.HasJournal = hasJournal == null ? false : hasJournal.Value; 
                    data.IsFavorite = IsFavorite == null ? false : IsFavorite.Value;
                    data.IsListenedTo = isListened == null ? false : isListened.Value;

                    db.InsertOrReplace(data);
                    Debug.WriteLine($"Added episode {episodeId}/{userName} to episode data table for later use...");
                }

                //Notify listening pages that episode data has changed 
                if (RaiseEpisodeDataChanged)
                {
                    MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                }
            }
            catch (Exception e)
            {
                //Getting Locked exception on android 
                Debug.WriteLine($"Exception in PlayerFeedAPI.UpdateEpisodeProperty(): {e.Message}");
                DabData.ResetDatabases();
                db = DabData.database;
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
                        cutoffTime = DateTime.Now.AddDays(-7);
                        break;
                        //case "One Month":
                        //	cutoffTime = DateTime.Now.AddMonths(-1);
                        //	break;
                }
                Debug.WriteLine("Cleaning up episodes...");
                List<dbEpisodes> episodesToDelete = new List<dbEpisodes>();
                if (OfflineEpisodeSettings.Instance.DeleteAfterListening)
                {
                    var eps = from x in db.Table<dbEpisodes>()
                              where x.is_downloaded  //downloaded episodes
                                          && (x.UserData.IsListenedTo == true || x.PubDate < cutoffTime)
                              select x;
                    episodesToDelete = eps.ToList();
                }
                else
                {
                    var eps = from x in db.Table<dbEpisodes>()
                              where x.is_downloaded && x.PubDate < cutoffTime
                              select x;
                    episodesToDelete = eps.ToList();
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
                            db.Update(episode);
                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update"); });
                            }
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
                var episode = db.Table<dbEpisodes>().Single(x => x.id == CurrentEpisodeId);
                episode.UserData.CurrentPosition = NewStopTime;
                episode.remaining_time = NewRemainingTime.ToString(); //TODO was a string - did making this a double break it?
                await adb.UpdateAsync(episode);
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
                dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
                dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
                dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
                dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
                dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
                var token = new APIToken
                {
                    value = TokenSettings.Value,
                    expires = ExpirationSettings.Value,
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
