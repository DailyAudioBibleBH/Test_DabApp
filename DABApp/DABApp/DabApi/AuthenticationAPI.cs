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
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = container.token;
				if (container.code == "login_error")
				{
					throw new Exception();
				}
				if (TokenSettings == null || EmailSettings == null)
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
			if (expiration == null) {
				return false;
			}
			DateTime expirationDate = DateTime.Parse(expiration.Value);
			if (expirationDate <= DateTime.Now) {
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
				var credentials = new FormUrlEncodedContent(new[] {
					new KeyValuePair<string, string>("user_email", email),
					new KeyValuePair<string, string>("user_first_name", firstName),
					new KeyValuePair<string, string>("user_last_name", lastName),
					new KeyValuePair<string, string>("user_password", password)
				});				
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/profile", credentials);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer contanier = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = contanier.token;
				if (contanier.code == "rest_forbidden") {
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
			LastNameSettings.Key = "LastName";
			LastNameSettings.Value = token.user_last_name;
			var AvatarSettings = new dbSettings();
			AvatarSettings.Key = "Avatar";
			AvatarSettings.Value = token.user_avatar;
			db.InsertOrReplace(TokenSettings);
			db.InsertOrReplace(ExpirationSettings);
			db.InsertOrReplace(EmailSettings);
			db.InsertOrReplace(FirstNameSettings);
			db.InsertOrReplace(LastNameSettings);
			db.InsertOrReplace(AvatarSettings);
		}
	}
}
