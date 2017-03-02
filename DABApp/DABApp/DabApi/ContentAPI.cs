using System;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DABApp
{
	public class ContentAPI
	{
		static SQLiteConnection db = DabData.database;

		private static int SynchTimeout = 10000;


		public static bool CheckContent() {
			var ContentSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "ContentJSON");
			var DataSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key=="data");
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
			ContentConfig.Instance = JsonConvert.DeserializeObject<ContentConfig>(jsonOut);
		}
	}
}
