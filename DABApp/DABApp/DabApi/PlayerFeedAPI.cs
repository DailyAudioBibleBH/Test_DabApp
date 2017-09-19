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
			//GetEpisodes(resource);
			return db.Table<dbEpisodes>().Where(x => x.channel_title == resource.title).OrderByDescending(x => x.PubDate);
		}

		public static async Task<string> GetEpisodes(Resource resource) {
			try
			{
				var client = new HttpClient();
				var result = await client.GetAsync(resource.feedUrl);
				string jsonOut = await result.Content.ReadAsStringAsync();
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
				await DownloadEpisodes();
				var check = await AuthenticationAPI.GetMemberData();
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
					//case "One Month":
					//	cutoffTime = DateTime.Now.AddMonths(-1);
					//	break;
				}

				//Get episodes to download
				var EpisodesToDownload = from channel in OfflineChannels
										 join episode in db.Table<dbEpisodes>() on channel.title equals episode.channel_title
										 where !episode.is_downloaded //not downloaded
									  && episode.PubDate > cutoffTime //new enough to be downloaded
									  && (!OfflineEpisodeSettings.Instance.DeleteAfterListening || episode.listenedToVisible) //not listened to or system not set to delete listened to episodes
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
						DownloadIsRunning = false;
						return false;
					}
				}
				Debug.WriteLine("Downloads complete!");
				DownloadIsRunning = false;
				Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update");});
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
				var ext = episode.url.Split('.').Last();
				if (DependencyService.Get<IFileManagement>().DeleteEpisode(episode.id.ToString(), ext))
				{
					episode.is_downloaded = false;
					db.Update(episode);
					MessagingCenter.Send<string>("Update", "Update");
				}
				else {
					throw new Exception();
				}
			}
		}

		public static void UpdateEpisodeProperty(int episodeId, string propertyName = null) {
			var episode = db.Table<dbEpisodes>().Single(x => x.id == episodeId);
			switch (propertyName)
			{
				case null:
					episode.is_listened_to = "listened";
					break;
				case "is_favorite":
					episode.is_favorite = !episode.is_favorite;
					break;
				case "has_journal":
					episode.has_journal = !episode.has_journal;
					break;
			}
			db.Update(episode);
			MessagingCenter.Send<string>("Update", "Update");
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
					//case "One Month":
					//	cutoffTime = DateTime.Now.AddMonths(-1);
					//	break;
				}
				Debug.WriteLine("Cleaning up episodes...");
				TableQuery<dbEpisodes> episodesToDelete;
				if (OfflineEpisodeSettings.Instance.DeleteAfterListening)
				{
					episodesToDelete = from x in db.Table<dbEpisodes>()
									   where x.is_downloaded  //downloaded episodes
												   && (x.is_listened_to == "listened" || x.PubDate < cutoffTime)
									   select x;
				}
				else
				{
					episodesToDelete = from x in db.Table<dbEpisodes>()
									   where x.is_downloaded && x.PubDate < cutoffTime
									   select x;
				}
				Debug.WriteLine("Cleaning up {0} episodes...", episodesToDelete.Count());
				foreach (var episode in episodesToDelete)
				{
					Debug.WriteLine("Cleaning up episode {0} ({1})...", episode.id, episode.url);
					try
					{
						var ext = episode.url.Split('.').Last();
						if (DependencyService.Get<IFileManagement>().DeleteEpisode(episode.id.ToString(), ext))
						{
							Debug.WriteLine("Episode {0} deleted.", episode.id, episode.url);
							episode.is_downloaded = false;
							db.Update(episode);
							Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Update", "Update");});
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
			MessagingCenter.Send<string>("Update", "Update");
		}

		public static async Task<Reading> GetReading(string ReadLink) {
			try
			{
				if (String.IsNullOrEmpty(ReadLink)) {
					throw new Exception();
				}
				HttpClient client = new HttpClient();
				var result = await client.GetAsync(ReadLink);
				var JsonOut = await result.Content.ReadAsStringAsync();
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
				reading.title = "An Http Request Exception has been called.  This may be due to problems with your network.  Please check your internet connection and try again.";
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
				var tres = await client.GetAsync("https://player.dailyaudiobible.com/donation/request_access");
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
				var result = await client.PostAsync("https://player.dailyaudiobible.com/donation/request_access", content);
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
				return $"Error: {e.Message}";
			}
		}
	}
}
