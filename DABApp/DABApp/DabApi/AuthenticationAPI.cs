using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQLite;

namespace DABApp
{
	public class AuthenticationAPI
	{
		static SQLiteConnection db = DabData.database;

		public static async Task<bool> ValidateLogin(string email, string password) {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
				HttpClient client = new HttpClient();
				StringContent content = new StringContent(string.Format("user_email={0}, user_password={1}", email, password));
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APIToken token = JsonConvert.DeserializeObject<APIToken>(JsonOut);
				if (token.code == "login_error")
				{
					throw new Exception();
				}
				if (TokenSettings == null)
				{
					TokenSettings = new dbSettings();
					TokenSettings.Key = "Token";
					TokenSettings.Value = token.value;
					ExpirationSettings = new dbSettings();
					ExpirationSettings.Key = "TokenExpiration";
					ExpirationSettings.Value = token.expires;
					db.Insert(TokenSettings);
					db.InsertOrReplace(ExpirationSettings);
				}
				else
				{
					TokenSettings.Value = token.value;
					ExpirationSettings.Value = token.expires;
				}
				return true;
			}
			catch (Exception e) {
				return false;
			}
		}

		public static bool CheckToken() {
			var expiration = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
			DateTime expirationDate = Convert.ToDateTime(expiration);
			if (expiration == null || expirationDate <= DateTime.Now) {
				return false;
			}
			return true;
		}
	}
}
