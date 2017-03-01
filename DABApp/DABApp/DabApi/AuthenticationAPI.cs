using System;
using System.Collections.Generic;
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
				dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
				dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
				dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
				dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
				HttpClient client = new HttpClient();
				var credentials = new FormUrlEncodedContent(new[] {
					new KeyValuePair<string, string>("user_email", email),
					new KeyValuePair<string, string>("user_password", password)
				});
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member", credentials);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APIToken token = JsonConvert.DeserializeObject<APIToken>(JsonOut);
				if (token.code == "login_error")
				{
					throw new Exception();
				}
				if (TokenSettings == null)
				{
					CreateSettings(token);
				}
				else
				{
					TokenSettings.Value = token.value;
					ExpirationSettings.Value = token.expires;
					EmailSettings.Value = token.user_email;
					FirstNameSettings.Value = token.user_first_name;
					LastNameSettings.Value = token.user_last_name;
					AvatarSettings.Value = token.user_avatar;
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

		public static async Task<bool> CreateNewMember(string firstName, string lastName, string email, string password) {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
				dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
				dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
				dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
				dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
				HttpClient client = new HttpClient();
				StringContent content = new StringContent(String.Format("user_email={0}, user_first_name={1}, user_last_name={2}, user_password={3}", email, firstName, lastName, password));
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/profile", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APIToken token = JsonConvert.DeserializeObject<APIToken>(JsonOut);
				if (token.code == "rest_forbidden") {
					throw new Exception();
				}
				if (TokenSettings == null)
				{
					CreateSettings(token);
				}
				else {
					TokenSettings.Value = token.value;
					ExpirationSettings.Value = token.expires;
					EmailSettings.Value = token.user_email;
					FirstNameSettings.Value = token.user_first_name;
					LastNameSettings.Value = token.user_last_name;
					AvatarSettings.Value = token.user_avatar;
				}
				return true;
			}
			catch (Exception e) {
				return false;
			}
		}

		static void CreateSettings(APIToken token) 
		{
			var TokenSettings = new dbSettings();
			TokenSettings.Key = "Token";
			TokenSettings.Value = token.value;
			var ExpirationSettings = new dbSettings();
			ExpirationSettings.Key = "TokenExpiration";
			ExpirationSettings.Value = token.expires;
			var EmailSettings = new dbSettings();
			EmailSettings.Key = "Email";
			EmailSettings.Value = token.user_email;
			var FirstNameSettings = new dbSettings();
			FirstNameSettings.Key = "FirstName";
			FirstNameSettings.Value = token.user_first_name;
			var LastNameSettings = new dbSettings();
			db.Insert(TokenSettings);
			db.InsertOrReplace(ExpirationSettings);
			db.InsertOrReplace(EmailSettings);
		}
	}
}
