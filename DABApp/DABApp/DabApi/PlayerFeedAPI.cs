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

		public static string GetEpisodes(string feedUrl, string Title) {
			try
			{
				var client = new HttpClient();
				var result = client.GetAsync(feedUrl).Result;
				string jsonOut = result.Content.ReadAsStringAsync().Result;
				var Episodes = JsonConvert.DeserializeObject<List<dbEpisodes>>(jsonOut);
				if (Episodes == null) {
					return "Server Error";
				}
				var existingEpisodes = db.Table<dbEpisodes>().Where(x => x.channel_title == Title).ToList();
				var tobeAdded = Episodes.Except(existingEpisodes).ToList();
				var tobeDeleted = existingEpisodes.Except(Episodes).ToList();
				db.InsertAll(tobeAdded);
				foreach (var old in tobeDeleted) {
					db.Delete(old);
				}
				return "OK";
			}
			catch (Exception e)
			{
				return e.Message;	
			}
		}
	}
}
