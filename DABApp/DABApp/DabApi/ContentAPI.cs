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
using System.Data.Common;
using static DABApp.ContentConfig;
using DABApp.Service;
using DABApp.DabSockets;
using System.Collections.ObjectModel;

namespace DABApp
{
    public class ContentAPI
    {
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;
        public static object cursur { get; set; } = null;

        public static bool CheckContent()
        {

            //Get the content API from the settings database
            string ContentSettings = dbSettings.GetSetting("ContentJSON", "");
            string DataSettings = dbSettings.GetSetting("data", "");

            if (ContentConfig.Instance.app_settings == null && ContentSettings != "")
            {
                ParseContent(ContentSettings);
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

                //Save Json Objects of Countries and States as a dbSettings
                JObject countryAndStateParse = JObject.Parse(jsonOut);
                JToken[] coutnryAndStateResults = countryAndStateParse["countries"].Children().ToArray();
                List<dynamic> countryAndStatesList = new List<dynamic>();

                foreach (JToken results in coutnryAndStateResults)
                {
                    foreach (var res in results)
                    {
                        var y = results.ToString();
                        var x = JsonConvert.SerializeObject(res);
                        string decoded;
                        if (y.Contains("names"))
                        {
                            decoded = System.Net.WebUtility.HtmlDecode(x);
                            dbSettings.StoreSetting("Country", decoded);
                        }
                        if (y.Contains("labels"))
                        {
                            decoded = System.Net.WebUtility.HtmlDecode(x);
                            dbSettings.StoreSetting("Labels", decoded);
                        }
                        if (y.Contains("states"))
                        {
                            decoded = System.Net.WebUtility.HtmlDecode(x);
                            dbSettings.StoreSetting("States", decoded);
                        }
                    }
                }

                //Showing that values saved and can easily be converted to a dictionary
                dbSettings CountrySettings = adb.Table<dbSettings>().Where(x => x.Key == "Country").FirstOrDefaultAsync().Result;
                dbSettings LabelSettings = adb.Table<dbSettings>().Where(x => x.Key == "Labels").FirstOrDefaultAsync().Result;
                dbSettings StateSettings = adb.Table<dbSettings>().Where(x => x.Key == "States").FirstOrDefaultAsync().Result;

                Dictionary<string, object> countryDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(CountrySettings.Value);
                Dictionary<string, object> labelDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(LabelSettings.Value);
                Dictionary<string, object> stateDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(StateSettings.Value);

                if (ContentSettings == null || DataSettings == null || ContentSettings == "" || DataSettings == "")
                {
                    dbSettings.StoreSetting("ContentJSON", jsonOut);
                    dbSettings.StoreSetting("data", updated);
                    ParseContent(jsonOut);
                }
                else
                {
                    if (DataSettings == updated)
                    {
                        ParseContent(ContentSettings);
                    }
                    else
                    {
                        DataSettings = updated;
                        ContentSettings = jsonOut;
                        ParseContent(jsonOut);
                    }
                }
                PlayerFeedAPI.CheckOfflineEpisodeSettings();
                return true;
            }
            catch (Exception ex)
            {
                if (ContentSettings == "")
                {
                    return false;
                }
                else
                {
                    ParseContent(ContentSettings);
                    return true;
                }
            }
        }

        public static void ParseContent(string jsonOut)
        {
            string OfflineSettingsValue = dbSettings.GetSetting("AvailableOffline", "");
            ContentConfig.Instance = JsonConvert.DeserializeObject<ContentConfig>(jsonOut);
            
            //Task.Run(async () =>
            //{
            //    await ContentConfig.Instance.cachImages();
            //});
            if (OfflineSettingsValue == "")
            {
                dbSettings.StoreSetting("AvailableOffline", new JArray().ToString());
            }
            else
            {
                List<int> ids = JsonConvert.DeserializeObject<List<int>>(OfflineSettingsValue);
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
                var OfflineSettingsValue = dbSettings.GetSetting("AvailableOffline", "");//adb.Table<dbSettings>().Where(x => x.Key == "AvailableOffline").FirstOrDefaultAsync().Result;
                if (OfflineSettingsValue != "")
                {
                    var jsonArray = JArray.Parse(OfflineSettingsValue);
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
                    dbSettings.StoreSetting("AvailableOffline", jsonArray.ToString());
                    Debug.WriteLine("Updated Offline settings");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static async Task<Forum> GetForum()
        {
            try
            {
                var forum = new Forum();
                forum.title = GlobalResources.ActiveForum.title;
                forum.topicCount = GlobalResources.ActiveForum.topicCount;
                var result = await DabService.GetUpdatedTopics(GlobalResources.ActiveForumId, 100, cursur);
                List<DabGraphQlTopic> topics = new List<DabGraphQlTopic>();
                if (result.Success)
                {
                    foreach (var item in result.Data)
                    {
                        topics = item.payload.data.updatedTopics.edges.Where(x => x.status == "publish").ToList();
                    }
                }
                ObservableCollection<DabGraphQlTopic> topicCollection = new ObservableCollection<DabGraphQlTopic>(topics);
                forum.topics = topicCollection;

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

                string TokenSettingsValue = GlobalResources.Instance.LoggedInUser.Token;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettingsValue);
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

                string TokenSettingsValue = GlobalResources.Instance.LoggedInUser.Token;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettingsValue);
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
