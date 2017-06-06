using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using SQLite;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Diagnostics;

namespace DABApp
{
	public class PlayerFeedAPI
	{
		static SQLiteConnection db = DabData.database;
		static bool DownloadIsRunning = false;
		static bool CleanupIsRunning = false;

		public static IEnumerable<dbEpisodes> GetEpisodeList(Resource resource) {
			GetEpisodes(resource);
			return db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate);
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
				foreach (var old in existingEpisodes)
				{
					if (!newEpisodeIds.Contains(old.id))
					{
						db.Delete(old);
					}
				}
				Task.Run( async () =>
				{
					await DownloadEpisodes();
				});
				var check = AuthenticationAPI.GetMemberData();
				return "OK";
				//else {
				//	throw new Exception(); 
				//}
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

		public static void UpdateOfflineEpisodeSettings() {
			var offlineSettings = db.Table<dbSettings>().Single(x => x.Key == "OfflineEpisodes");
			offlineSettings.Value = JsonConvert.SerializeObject(OfflineEpisodeSettings.Instance);
			db.Update(offlineSettings);
		}

		public static async Task<bool> DownloadEpisodes() {

			if (!DownloadIsRunning)
			{
				DownloadIsRunning = true;
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
						cutoffTime = DateTime.Now.AddDays(-7);
						break;
					case "One Month":
						cutoffTime = DateTime.Now.AddMonths(-1);
						break;
				}

				//Get episodes to download
				var EpisodesToDownload = from channel in OfflineChannels
										 join episode in db.Table<dbEpisodes>() on channel.title equals episode.channel_title
										 where !episode.is_downloaded //not downloaded
									  && episode.PubDate > cutoffTime //new enough to be downloaded
									  && (!OfflineEpisodeSettings.Instance.DeleteAfterListening || !episode.is_listened_to) //not listened to or system not set to delete listened to episodes
										 select episode;

				int ix = 0;
				foreach (var episode in EpisodesToDownload.ToList())
				{
					ix++;
					try
					{
						Debug.WriteLine("Starting to download episode {0} ({1}/{2} - {3})...", episode.id, ix, EpisodesToDownload.Count(), episode.url);
						if (await DependencyService.Get<IFileManagement>().DownloadEpisodeAsync(episode.url, episode.id.ToString()))
						{
							Debug.WriteLine("Finished downloading episode {0} ({1})...", episode.id, episode.url);
							episode.is_downloaded = true;
							db.Update(episode);
						}
						else throw new Exception();
					}
					catch (Exception e)
					{
						Debug.WriteLine("Error while downloading episode {0} ({1}): {2}", episode.id, episode.url, e.ToString());
						return false;
					}
				}
				Debug.WriteLine("Downloads complete!");
				DownloadIsRunning = false;
				return true;
			}
			else {
				//download is already running
				return true;
			}
		}

		public static void DeleteChannelEpisodes(Resource resource) {
			var Episodes = db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title && x.is_downloaded).ToList();
			foreach (var episode in Episodes) {
				if (DependencyService.Get<IFileManagement>().DeleteEpisode(episode.id.ToString()))
				{
					episode.is_downloaded = false;
					db.Update(episode);
				}
				else {
					throw new Exception();
				}
			}
		}

		public static void UpdateEpisodeProperty(int episodeId) {
			var episode = db.Table<dbEpisodes>().Single(x => x.id == episodeId);
			episode.is_listened_to = true;
			db.Update(episode);
		}

		public static void CleanUpEpisodes() {
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
					case "One Month":
						cutoffTime = DateTime.Now.AddMonths(-1);
						break;
				}
				Debug.WriteLine("Cleaning up episodes...");
				var episodesToDelete = from x in db.Table<dbEpisodes>()
									   where x.is_downloaded  //downloaded episodes
												&& x.PubDate < cutoffTime //pubDate is before cut off time
												&& (!OfflineEpisodeSettings.Instance.DeleteAfterListening //not flagged to delete after listening
														||
														(OfflineEpisodeSettings.Instance.DeleteAfterListening || x.is_listened_to)) //flagged to delete after listening and listened to
									   select x;
				Debug.WriteLine("Cleaning up {0} episodes...", episodesToDelete.Count());
				foreach (var episode in episodesToDelete)
				{
					Debug.WriteLine("Cleaning up episode {0} ({1})...", episode.id, episode.url);
					try
					{
						if (DependencyService.Get<IFileManagement>().DeleteEpisode(episode.id.ToString()))
						{
							Debug.WriteLine("Episode {0} deleted.", episode.id, episode.url);
							episode.is_downloaded = false;
							db.Update(episode);
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

		public static void UpdateStopTime(int CurrentEpisodeId, double NewStopTime, string NewRemainingTime) {
			var episode = db.Table<dbEpisodes>().Single(x => x.id == CurrentEpisodeId);
			episode.stop_time = NewStopTime;
			episode.remaining_time = NewRemainingTime;
			db.Update(episode);
		}

		public static Reading GetReading(string ReadLink) {
			try
			{
				HttpClient client = new HttpClient();
				var result = client.GetAsync(ReadLink).Result;
				var JsonOut = result.Content.ReadAsStringAsync().Result;
				var content = JsonConvert.DeserializeObject<Reading>(JsonOut);
				if (content.message != null)
				{
					throw new Exception(content.message);
				}
				if (content.link != ReadLink) {
					content.IsAlt = true;
				}
				return content;
			}
			catch (HttpRequestException re) {
				var reading = new Reading();
				reading.title = re.Message;
				return reading;
			}
			catch (Exception e) {
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
					reading.title = e.Message;
				}
				return reading;
			}
		} 
	}
}
