using System;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading;

namespace DABApp
{
	public class ContentAPI
	{
		static SQLiteConnection db = DabData.database;

		public static bool CheckContent() {
			var ContentSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "ContentJSON");
			var DataSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key=="data");
			var OfflineSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key =="AvailableOffline");
			try
			{
				var client = new System.Net.Http.HttpClient();
				HttpResponseMessage result;
				string jsonOut = "";
				Task.Run(async () =>
				{
					var r = client.GetAsync("https://feed.dailyaudiobible.com/wp-json/lutd/v1/content");
					if (await Task.WhenAny(r, Task.Delay(TimeSpan.FromSeconds(8))) == r)
					{
						result = await r;
						jsonOut = await result.Content.ReadAsStringAsync();
					}
					else throw new Exception();
				}).Wait();//Appended the GUID to avoid caching.
				var updated = JsonConvert.DeserializeObject<ContentConfig>(jsonOut).data.updated;
				if (ContentSettings == null || DataSettings == null)
				{
					ContentSettings = new dbSettings();
					ContentSettings.Key = "ContentJSON";
					ContentSettings.Value = jsonOut;
					DataSettings = new dbSettings();
					DataSettings.Key = "data";
					DataSettings.Value = updated;
					db.Insert(ContentSettings);
					db.Insert(DataSettings);

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
			catch (Exception) {
				if (ContentSettings == null)
				{
					return false;
				}
				else {
					ParseContent(ContentSettings.Value);
					return true;
				}
			}
		}

		public static void ParseContent(string jsonOut)
		{			
			var OfflineSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "AvailableOffline");
			ContentConfig.Instance = JsonConvert.DeserializeObject<ContentConfig>(jsonOut);
			ContentConfig.Instance.cachImages();
			if (OfflineSettings == null)
			{
				OfflineSettings = new dbSettings();
				OfflineSettings.Key = "AvailableOffline";
				OfflineSettings.Value = new JArray().ToString();
				db.Insert(OfflineSettings);
			}
			else {
				List<int> ids = JsonConvert.DeserializeObject<List<int>>(OfflineSettings.Value);
				List<Resource> resources = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Where(x => ids.Contains(x.id)).ToList();
				foreach (var item in resources) {
					item.availableOffline = true;
				}
			}
		}

		public static void UpdateOffline(bool offline, int ResourceId) {
			var OfflineSettings = db.Table<dbSettings>().Single(x => x.Key == "AvailableOffline");
			var jsonArray = JArray.Parse(OfflineSettings.Value);
			if (offline || !jsonArray.Contains(ResourceId))
			{
				jsonArray.Add(ResourceId);
			}
			else {
				if (jsonArray.Contains(ResourceId)) {
					jsonArray.Remove(ResourceId);
				}
			}
			OfflineSettings.Value = jsonArray.ToString();
			db.Update(OfflineSettings);
		}

		public static async Task<Forum> GetForum(View view) 
		{
			try
			{
				var client = new HttpClient();
				var result = await client.GetAsync(view.resources.First().feedUrl);
				var JsonOut = await result.Content.ReadAsStringAsync();
				var forum = JsonConvert.DeserializeObject<Forum>(JsonOut);
				return forum;
			}
			catch (Exception e)
			{
				return null;
			}
		}

		public static async Task<Topic> GetTopic(Topic topic)
		{
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
				return null;
			}
		}

		public static async Task<string> PostTopic(PostTopic topic)
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				var client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(topic);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/topics", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (!JsonOut.Contains("id")) {
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
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				var client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(reply);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/replies", content);
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
