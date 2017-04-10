using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using SQLite;
using System.Threading.Tasks;

namespace DABApp
{
	public class PlayerFeedAPI
	{
		static SQLiteConnection db = DabData.database;

		public static List<dbEpisodes> GetEpisodeList(Resource resource) {
			GetEpisodes(resource);
			return db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate).ToList();
		}

		public static string GetEpisodes(Resource resource) {
			try
			{
				var client = new HttpClient();
				var result = client.GetAsync(resource.feedUrl).Result;
				string jsonOut = result.Content.ReadAsStringAsync().Result;
				var Episodes = JsonConvert.DeserializeObject<List<dbEpisodes>>(jsonOut);
				if (Episodes == null) {
					return "Server Error";
				}
				var existingEpisodes = db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).ToList();
				var existingEpisodeIds = existingEpisodes.Select(x => x.id);
				var newEpisodeIds = Episodes.Select(x => x.id);
				foreach (var e in Episodes) {
					if (!existingEpisodeIds.Contains(e.id)) {
						db.Insert(e);
					}
				}
				foreach (var old in existingEpisodes) {
					if (!newEpisodeIds.Contains(old.id)) {
						db.Delete(old);
					}
				}
				return "OK";
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		public static dbEpisodes GetMostRecentEpisode(Resource resource) 
		{
			var episode = db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate).FirstOrDefault();
			return episode;
		}

		public static dbEpisodes GetEpisode(int id) {
			return db.Table<dbEpisodes>().Single(x => x.id == id);
		}

		public static void CheckOfflineEpisodeSettings() {
			var offlineSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "OfflineEpisodes");
			if (offlineSettings == null)
			{
				offlineSettings = new dbSettings();
				offlineSettings.Key = "OfflineEpisodes";
				OfflineEpisodeSettings settings = new OfflineEpisodeSettings();
				settings.Duration = "One Week";
				settings.DeleteAfterListening = false;
				var jsonSettings = JsonConvert.SerializeObject(settings);
				offlineSettings.Value = jsonSettings;
				db.Insert(offlineSettings);
			}
			else
			{
				var current = offlineSettings.Value;
				OfflineEpisodeSettings.Instance = JsonConvert.DeserializeObject<OfflineEpisodeSettings>(current);
			}
		}

		public static void UpdateOfflineEpisodeSettings() {
			var offlineSettings = db.Table<dbSettings>().Single(x => x.Key == "OfflineEpisodes");
			offlineSettings.Value = JsonConvert.SerializeObject(OfflineEpisodeSettings.Instance);
			db.Update(offlineSettings);
		}
	}
}
