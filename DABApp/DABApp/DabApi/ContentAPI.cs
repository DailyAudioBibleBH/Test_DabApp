using System;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DABApp
{
	public class ContentAPI
	{
		static SQLiteConnection db = DabData.database;

		private static int SynchTimeout = 10000;


		public static bool CheckContent() {
			var ContentSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "ContentJSON");
			var DataSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key=="data");
			var OfflineSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key =="AvailableOffline");
			try{
				var client = new System.Net.Http.HttpClient();
				var result = client.GetAsync("https://feed.dailyaudiobible.com/wp-json/lutd/v1/content?" + Guid.NewGuid().ToString()).Result; //Appended the GUID to avoid caching.
				string jsonOut = result.Content.ReadAsStringAsync().Result;
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
				else {
					if (DataSettings.Value == updated)
					{
						ParseContent(ContentSettings.Value);
					}
					else {
						DataSettings.Value = updated;
						ContentSettings.Value = jsonOut;
						ParseContent(jsonOut);
					}
				}
				PlayerFeedAPI.CheckOfflineEpisodeSettings();
				return true;
			}
			catch (Exception e) {
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
			if (offline)
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
	}
}
