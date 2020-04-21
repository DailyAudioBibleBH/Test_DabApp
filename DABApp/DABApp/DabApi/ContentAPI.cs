using System;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using Xamarin.Forms;
using System.Runtime.Serialization.Json;
using Xamarin.Forms.PlatformConfiguration;

namespace DABApp
{
    public class ContentAPI
    {
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;

        public static bool CheckContent()
        {

            //Get the content API from the settings database
            dbSettings ContentSettings = adb.Table<dbSettings>().Where(x => x.Key == "ContentJSON").FirstOrDefaultAsync().Result;
            dbSettings DataSettings = adb.Table<dbSettings>().Where(x => x.Key == "data").FirstOrDefaultAsync().Result;

            if (ContentConfig.Instance.app_settings == null && ContentSettings != null)
            {
                ParseContent(ContentSettings.Value);
            }
            try
            {
                //Try to get a fresh copy of the Content API
                var client = new HttpClient();
                HttpResponseMessage result;
                string jsonOut = "";
                Task.Run(async () =>
                {
                    var r = client.GetAsync($"{GlobalResources.FeedAPIUrl}content");
                    if (await Task.WhenAny(r, Task.Delay(TimeSpan.FromSeconds(8))) == r)
                    {
                        result = await r;
                        jsonOut = await result.Content.ReadAsStringAsync();
                    }
                    else throw new Exception("Request for Content API timed out.");
                }).Wait();//Appended the GUID to avoid caching.
                var updated = JsonConvert.DeserializeObject<ContentConfig>(jsonOut).data.updated;
                var test = JsonConvert.DeserializeObject<ContentConfig>(jsonOut);
                if (ContentSettings == null || DataSettings == null)
                {
                    ContentSettings = new dbSettings();
                    ContentSettings.Key = "ContentJSON";
                    ContentSettings.Value = jsonOut;
                    DataSettings = new dbSettings();
                    DataSettings.Key = "data";
                    DataSettings.Value = updated;
                    adb.InsertOrReplaceAsync(ContentSettings);
                    adb.InsertOrReplaceAsync(DataSettings);

                    ParseContent(jsonOut);
                }
                else
                {
                    if (DataSettings.Value == updated)
                    {
                        ParseContent(ContentSettings.Value);
                    }
                    else
                    {
                        DataSettings.Value = updated;
                        ContentSettings.Value = jsonOut;
                        ParseContent(jsonOut);
                    }
                }
                PlayerFeedAPI.CheckOfflineEpisodeSettings();
                return true;
            }
            catch (Exception)
            {
                if (ContentSettings == null)
                {
                    return false;
                }
                else
                {
                    ParseContent(ContentSettings.Value);
                    return true;
                }
            }
        }

        public static void ParseContent(string jsonOut)
        {
            var OfflineSettings = adb.Table<dbSettings>().Where(x => x.Key == "AvailableOffline").FirstOrDefaultAsync().Result;
            ContentConfig.Instance = JsonConvert.DeserializeObject<ContentConfig>(jsonOut);
            
            Task.Run(async () =>
            {
                await ContentConfig.Instance.cachImages();
            });
            if (OfflineSettings == null)
            {
                OfflineSettings = new dbSettings();
                OfflineSettings.Key = "AvailableOffline";
                OfflineSettings.Value = new JArray().ToString();
                adb.InsertAsync(OfflineSettings);
            }
            else
            {
                List<int> ids = JsonConvert.DeserializeObject<List<int>>(OfflineSettings.Value);
                List<Resource> resources = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Where(x => ids.Contains(x.id)).ToList();
                foreach (var item in resources)
                {
                    item.availableOffline = true;
                }
            }
        }

        public static async Task UpdateOffline(bool offline, int ResourceId)
        {
            try
            {
                Debug.WriteLine("Updating Offline Settings");
                var OfflineSettings = adb.Table<dbSettings>().Where(x => x.Key == "AvailableOffline").FirstOrDefaultAsync().Result;
                if (OfflineSettings != null)
                {
                    var jsonArray = JArray.Parse(OfflineSettings.Value);
                    var match = jsonArray.Where(j => j.ToString().Equals(ResourceId.ToString()));
                    if (offline && match.Count() == 0)
                    {
                        jsonArray.Add(ResourceId);
                    }
                    else
                    {
                        if (!offline && match.Count() > 0)
                        {
                            for (int m = match.Count() - 1; m >= 0; m--)
                            {
                                jsonArray.Remove(match.ElementAt(m));
                            }
                        }
                    }
                    OfflineSettings.Value = jsonArray.ToString();
                    await adb.UpdateAsync(OfflineSettings);
                    Debug.WriteLine("Updated Offline settings");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static async Task<Forum> GetForum(View view, int pageNumber = 1)
        {
            try
            {
                var client = new HttpClient();
                var result = await client.GetAsync($"{view.resources.First().feedUrl}?page={pageNumber}&perpage=50");
                var JsonOut = await result.Content.ReadAsStringAsync();
                var forum = JsonConvert.DeserializeObject<Forum>(JsonOut);
                forum.view = view;
                return forum;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<Topic> GetTopic(Topic topic)
        {
            DependencyService.Get<IAnalyticsService>().LogEvent("prayerwall_post_read");
            try
            {
                var client = new HttpClient();
                var result = await client.GetAsync(topic.link);
                var JsonOut = await result.Content.ReadAsStringAsync();
                var top = JsonConvert.DeserializeObject<Topic>(JsonOut);
                return top;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception caught in GetTopic: {e.Message}");
                return null;
            }
        }

        public List<modeData> GetModes()
        {
            List<Versions> versionsList = new List<Versions>();
            var mode = from version in versionsList
                       where version.mode != null
                       select version.mode;

            return mode.ToList();
        }

        public static async Task<string> PostTopic(PostTopic topic)
        {
            try
            {
                //Sending Event to Firebase Analytics about Topic post
                DependencyService.Get<IAnalyticsService>().LogEvent("prayerwall_post_written");

                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                var JsonIn = JsonConvert.SerializeObject(topic);
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}topics", content);
                string JsonOut = await result.Content.ReadAsStringAsync();
                if (!JsonOut.Contains("id"))
                {
                    var error = JsonConvert.DeserializeObject<APIError>(JsonOut);
                    throw new Exception(error.message);
                }
                return JsonOut;
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(HttpRequestException))
                {
                    return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
                }
                return e.Message;
            }
        }

        public static async Task<string> PostReply(PostReply reply)
        {
            try
            {
                //Sending Event to Firebase Analytics to record Reply post.
                DependencyService.Get<IAnalyticsService>().LogEvent("prayerwall_post_replied");

                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                var JsonIn = JsonConvert.SerializeObject(reply);
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}replies", content);
                string JsonOut = await result.Content.ReadAsStringAsync();
                if (!JsonOut.Contains("id"))
                {
                    var error = JsonConvert.DeserializeObject<APIError>(JsonOut);
                    throw new Exception(error.message);
                }
                return JsonOut;
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(HttpRequestException))
                {
                    return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
                }
                return e.Message;
            }
        }
    }
}
