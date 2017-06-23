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

		public static async Task<string> ValidateLogin(string email, string password, bool IsGuest = false) {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
				dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
				dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
				dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
				dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
				if (IsGuest) {
					if (EmailSettings == null)
					{
						APIToken Empty = new APIToken();
						Empty.user_avatar = "";
						Empty.user_email = "Guest";
						Empty.user_first_name = "";
						Empty.user_last_name = "";
						Empty.value = "";
						Empty.expires = DateTime.Now.ToString();
						CreateSettings(Empty);
					}
					else {
						TokenSettings.Value = "";
						EmailSettings.Value = "Guest";
						ExpirationSettings.Value = DateTime.Now.ToString();
						FirstNameSettings.Value = "";
						LastNameSettings.Value = "";
						AvatarSettings.Value = "";
						IEnumerable<dbSettings> settings = Enumerable.Empty<dbSettings>();
						settings = new dbSettings[] { TokenSettings, ExpirationSettings, EmailSettings, FirstNameSettings, LastNameSettings, AvatarSettings };
						db.UpdateAll(settings, true);
					}
					return "IsGuest";
				}
				else
				{
					HttpClient client = new HttpClient();
					var JsonIn = JsonConvert.SerializeObject(new LoginInfo(email, password));
					var content = new StringContent(JsonIn);
					content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
					var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member", content);
					string JsonOut = await result.Content.ReadAsStringAsync();
					APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
					APIToken token = container.token;
					if (container.code == "login_error")
					{
						return container.message;
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
						IEnumerable<dbSettings> settings = Enumerable.Empty<dbSettings>();
						settings = new dbSettings[] { TokenSettings, ExpirationSettings, EmailSettings, FirstNameSettings, LastNameSettings, AvatarSettings };
						db.UpdateAll(settings, true);
						//GuestStatus.Current.AvatarUrl = new Uri(token.user_avatar);
						GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
					}
					return container.message;
				}
			}
			catch (Exception e) {
				if (e.GetType() == typeof(HttpRequestException))
				{
					return e.Message;
				}
				else return e.Message;
			}
		}

		public static bool CheckToken(int days = 0) {
			var expiration = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
			if (expiration == null) {
				return false;
			}
			if (expiration.Value == null) return false;
			DateTime expirationDate = DateTime.Parse(expiration.Value);
			if (expirationDate <= DateTime.Now.AddDays(days)) {
				return false;
			}
			return true;
		}

		public static async Task<string> CreateNewMember(string firstName, string lastName, string email, string password) {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
				dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
				dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
				dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
				dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvcmVzdC5kYWlseWF1ZGlvYmlibGUuY29tIiwiaWF0IjoxNDg4NDc1NTI3LCJuYmYiOjE0ODg0NzU1MjcsImV4cCI6MTUyMDg3NTUyNywiZGF0YSI6eyJ1c2VyIjp7ImlkIjoiNzE5NiJ9fX0.1I9rftNgHoJYI8g3i1jeHqI7nLjBA0cVOjhe6O5Ayf8");
				var JsonIn = JsonConvert.SerializeObject(new SignUpInfo(email, firstName, lastName, password));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/profile", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = container.token;
				if (container.code == "rest_forbidden" || container.code == "add_member_error") {
					return "The following error was thrown by the server: " + container.message;
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
					IEnumerable<dbSettings> settings = new dbSettings[] { TokenSettings, ExpirationSettings, EmailSettings, FirstNameSettings, LastNameSettings, AvatarSettings };
					db.UpdateAll(settings, true);
					//GuestStatus.Current.AvatarUrl = new Uri(token.user_avatar);
					GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
				}
				return "";
			}
			catch (Exception e) {
				if (e.GetType() == typeof(HttpRequestException))
				{
					return "Http Request Timed out.";
				}
				else return "The following exception was caught: " + e.Message;
			}
		}

		public static async Task<string> ResetPassword(string email) {
			try
			{
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvcmVzdC5kYWlseWF1ZGlvYmlibGUuY29tIiwiaWF0IjoxNDg4NDc1NTI3LCJuYmYiOjE0ODg0NzU1MjcsImV4cCI6MTUyMDg3NTUyNywiZGF0YSI6eyJ1c2VyIjp7ImlkIjoiNzE5NiJ9fX0.1I9rftNgHoJYI8g3i1jeHqI7nLjBA0cVOjhe6O5Ayf8");
				var JsonIn = JsonConvert.SerializeObject(new ResetEmailInfo(email));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/resetpassword", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				return container.message;
			}
			catch (Exception e) {
				return "The following exception was caught: " + e.Message;
			}
		}

		public static async Task<bool> LogOut() {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().Single(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
				HttpClient client= new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvcmVzdC5kYWlseWF1ZGlvYmlibGUuY29tIiwiaWF0IjoxNDg4NDc1NTI3LCJuYmYiOjE0ODg0NzU1MjcsImV4cCI6MTUyMDg3NTUyNywiZGF0YSI6eyJ1c2VyIjp7ImlkIjoiNzE5NiJ9fX0.1I9rftNgHoJYI8g3i1jeHqI7nLjBA0cVOjhe6O5Ayf8");
				var JsonIn = JsonConvert.SerializeObject(new LogOutInfo(TokenSettings.Value));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/logout", content);
				if (result.StatusCode != System.Net.HttpStatusCode.OK) {
					throw new Exception();
				}
				ExpirationSettings.Value = DateTime.MinValue.ToString();
				db.Update(ExpirationSettings);
				return true;
			}
			catch (Exception e) {
				dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
				ExpirationSettings.Value = DateTime.MinValue.ToString();
				db.Update(ExpirationSettings);
				return false;
			}
		}

		public static async void ExchangeToken() {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().Single(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(new LogOutInfo(TokenSettings.Value));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/exchangetoken", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = container.token;
				if (container.token == null) {
					throw new Exception();
				}
				TokenSettings.Value = token.value;
				ExpirationSettings.Value = token.expires;
				db.Update(TokenSettings);
				db.Update(ExpirationSettings);
			}
			catch (Exception e) {

			}
		}

		public static async Task<string> EditMember(string email, string firstName, string lastName, string currentPassword, string newPassword, string confirmNewPassword) 
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
				dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
				dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
				dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(new EditProfileInfo(email, firstName, lastName, currentPassword, newPassword, confirmNewPassword));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PutAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/profile", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = container.token;
				if (container.message != null && token == null)
				{
					throw new Exception(container.message);
				}
				TokenSettings.Value = token.value;
				ExpirationSettings.Value = token.expires;
				EmailSettings.Value = token.user_email;
				FirstNameSettings.Value = token.user_first_name;
				LastNameSettings.Value = token.user_last_name;
				db.Update(TokenSettings);
				db.Update(ExpirationSettings);
				db.Update(EmailSettings);
				db.Update(FirstNameSettings);
				db.Update(LastNameSettings);
				return "Success";
			}
			catch (Exception e) {
				return e.Message;
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
			GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
		}

		public static void CreateNewActionLog(int episodeId, string actionType, double playTime) 
		{
			var actionLog = new dbPlayerActions();
			actionLog.ActionDateTime = DateTime.Now;
			actionLog.entity_type = "episode";
			actionLog.EpisodeId = episodeId;
			actionLog.PlayerTime = playTime;
			actionLog.ActionType = actionType;
			var user = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
			if (user != null) {
				actionLog.UserEmail = user.Value;
			}
			db.Insert(actionLog);
		}

		public static void PostActionLogs() {
			dbSettings TokenSettings = db.Table<dbSettings>().Single(x => x.Key == "Token");
			var actions = db.Table<dbPlayerActions>().ToList();
 			if (actions.Count > 0) {
				try
				{
					LoggedEvents events = new LoggedEvents();
					HttpClient client = new HttpClient();
					client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
					events.data = PlayerEpisodeAction.ParsePlayerActions(actions);
					var JsonIn = JsonConvert.SerializeObject(events);
					var content = new StringContent(JsonIn);
					content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
					var result = client.PostAsync("https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/logevents", content).Result;
					string JsonOut = result.Content.ReadAsStringAsync().Result;
					if (JsonOut != "1")
					{
						throw new Exception();
					}
					foreach (var action in actions)
					{
						db.Delete(action);
					}
				}
				catch (Exception e) 
				{
					//It's bad if the program lands here.
				}
			}
		}

		public static bool GetMemberData(){
			dbSettings TokenSettings = db.Table<dbSettings>().Single(x => x.Key == "Token");
			dbSettings EmailSettings = db.Table<dbSettings>().Single(x => x.Key == "Email");
			try
			{
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(EmailSettings.Value);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = client.GetAsync($"https://rest.dailyaudiobible.com/wp-json/lutd/v1/member/data").Result;
				string JsonOut = result.Content.ReadAsStringAsync().Result;
				MemberData container = JsonConvert.DeserializeObject<MemberData>(JsonOut);
				if (container.code == "rest_forbidden")
				{
					throw new Exception();
				}
				else {
					SaveMemberData(container.listened_episodes);
				}
				return true;
			}
			catch (Exception e) {
				return false;
			}
		}

		static void SaveMemberData(List<dbEpisodes> episodes) {
			foreach (dbEpisodes episode in episodes) {
				var saved = db.Table<dbEpisodes>().SingleOrDefault(x => x.id == episode.id);
				if (saved == null)
				{
					db.Insert(episode);
				}
				else {
					saved.stop_time = episode.stop_time;
					db.Update(saved);
				}
			}
		}
	}
}
