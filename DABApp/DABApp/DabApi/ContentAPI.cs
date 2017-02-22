using System;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DABApp
{
	public class ContentAPI
	{
		static SQLiteConnection db = DabData.database;

		private static int SynchTimeout = 10000;


		public static async Task<bool> CheckContent() {
			var settings = db.Table<dbSettings>().FirstOrDefault();
			try{
				var client = new System.Net.Http.HttpClient();
				var PreResult = client.GetAsync("https://feed.dailyaudiobible.com/wp-json/lutd/v1/content");
				if (!(await Task.WhenAny(PreResult, Task.Delay(SynchTimeout)) == PreResult))
				{
					throw new Exception();
				}
				var result = await PreResult;
				string jsonOut = await result.Content.ReadAsStringAsync();
				var updated = JsonConvert.DeserializeObject<ContentConfig>(jsonOut).data.updated;
				if (settings == null)
				{
					settings = new dbSettings();
					settings.Key = updated;
					settings.Value = jsonOut;
					db.Insert(settings);
					ParseContent(jsonOut, updated);
				}
				else {
					if (settings.Key == updated)
					{
						ParseContent(settings.Value, settings.Key);
					}
					else {
						settings.Key = updated;
						settings.Value = jsonOut;
						ParseContent(jsonOut, updated);
					}
				}
				return true;
			}
			catch (Exception e) {
				ParseContent(settings.Value, settings.Key);
				return false;
			}
		}

		public static void ParseContent(string jsonOut, string updated)
		{
			ContentConfig.Instance = JsonConvert.DeserializeObject<ContentConfig>(jsonOut);
		}
	}
}
