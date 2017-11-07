using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plugin.Connectivity;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public class AuthenticationAPI
	{
		static SQLiteConnection db = DabData.database;
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;

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
						await adb.UpdateAllAsync(settings);
					}
					return "IsGuest";
				}
				else
				{
					HttpClient client = new HttpClient();
					var JsonIn = JsonConvert.SerializeObject(new LoginInfo(email, password));
					var content = new StringContent(JsonIn);
					content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
					var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member", content);
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
						await adb.UpdateAllAsync(settings);
						//GuestStatus.Current.AvatarUrl = new Uri(token.user_avatar);
						GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
					}
					JournalTracker.Current.Connect(token.value);
					if (!string.IsNullOrEmpty(token.user_avatar)) GuestStatus.Current.AvatarUrl = token.user_avatar;
					return "Success";
				}
			}
			catch (Exception e) {
				if (e.GetType() == typeof(HttpRequestException))
				{
					return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
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
			var token = db.Table<dbSettings>().Single(x => x.Key == "Token");
			if (!JournalTracker.Current.IsConnected && CrossConnectivity.Current.IsConnected) {
				JournalTracker.Current.Connect(token.Value);
			}
			return true;
		}

		public static void ConnectJournal() 
		{
			try
			{
				if (!JournalTracker.Current.IsConnected && CrossConnectivity.Current.IsConnected)
				{
					var token = db.Table<dbSettings>().Single(x => x.Key == "Token");
					JournalTracker.Current.Connect(token.Value);
				}
			}
			catch(Exception e)
			{
				Debug.WriteLine($"Exception caught in AuthenticationAPI.ConnectJournal(): {e.Message}");
			}
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
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalResources.APIKey);
				var JsonIn = JsonConvert.SerializeObject(new SignUpInfo(email, firstName, lastName, password));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/profile", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = container.token;
				if (container.code == "rest_forbidden" || container.code == "add_member_error") {
					return "An error occured: " + container.message;
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
					await adb.UpdateAllAsync(settings);
					//GuestStatus.Current.AvatarUrl = new Uri(token.user_avatar);
					GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
				}
				JournalTracker.Current.Connect(TokenSettings.Value);
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
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalResources.APIKey);
				var JsonIn = JsonConvert.SerializeObject(new ResetEmailInfo(email));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/resetpassword", content);
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
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalResources.APIKey);
				var JsonIn = JsonConvert.SerializeObject(new LogOutInfo(TokenSettings.Value));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/logout", content);
				if (result.StatusCode != System.Net.HttpStatusCode.OK) {
					throw new Exception();
				}
				ExpirationSettings.Value = DateTime.MinValue.ToString();
				await adb.UpdateAsync(ExpirationSettings);
				return true;
			}
			catch (Exception e) {
				dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
				ExpirationSettings.Value = DateTime.MinValue.ToString();
				await adb.UpdateAsync(ExpirationSettings);
				return false;
			}
		}

		public static async Task<bool> ExchangeToken() {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().Single(x => x.Key == "Token");
				dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(new LogOutInfo(TokenSettings.Value));
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/exchangetoken", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
				APIToken token = container.token;
				if (container.token == null) {
					throw new Exception();
				}
				TokenSettings.Value = token.value;
				ExpirationSettings.Value = token.expires;
				await adb.UpdateAsync(TokenSettings);
				await adb.UpdateAsync(ExpirationSettings);
				JournalTracker.Current.Connect(token.value);
				return true;
			}
			catch (Exception e) {
				return false;
			}
		}

		public static async Task<bool> GetMember() 
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
				dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
				dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}member/profile");
				string JsonOut = await result.Content.ReadAsStringAsync();
				ProfileInfo info = JsonConvert.DeserializeObject<ProfileInfo>(JsonOut);
				if (info.email == null)
				{
					throw new Exception();
				}
				EmailSettings.Value = info.email;
				FirstNameSettings.Value = info.first_Name;
				LastNameSettings.Value = info.last_Name;
				await adb.UpdateAsync(EmailSettings);
				await adb.UpdateAsync(FirstNameSettings);
				await adb.UpdateAsync(LastNameSettings);
				return true;
			}
			catch (Exception e) 
			{
				return false;
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
				var result = await client.PutAsync($"{GlobalResources.RestAPIUrl}member/profile", content);
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
				await adb.UpdateAsync(TokenSettings);
				await adb.UpdateAsync(ExpirationSettings);
				await adb.UpdateAsync(EmailSettings);
				await adb.UpdateAsync(FirstNameSettings);
				await adb.UpdateAsync(LastNameSettings);
				return "Success";
			}
			catch (Exception e) {
				if (e.GetType() == typeof(HttpRequestException))
				{
					return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
				}
				return e.Message;
			}
		}

		public static async Task<APIAddresses> GetAddresses() {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}addresses");
				string JsonOut = await result.Content.ReadAsStringAsync();
				APIAddresses addresses = JsonConvert.DeserializeObject<APIAddresses>(JsonOut);
				if (addresses.billing == null) {
					throw new Exception();
				}
				return addresses;
			}
			catch (Exception e) {
				return null;
			}
		}

		public static async Task<Country[]> GetCountries() {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}countries");
				string JsonOut = await result.Content.ReadAsStringAsync();
				Country[] countries = JsonConvert.DeserializeObject<Country[]>(JsonOut);
				return countries;
			}
			catch (Exception e) {
				return null;
			}
		}

		public static async Task<string> UpdateBillingAddress(Address newBilling) {
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				var JsonIn = JsonConvert.SerializeObject(newBilling);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.PutAsync($"{GlobalResources.RestAPIUrl}addresses", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (JsonOut == "true")
				{
					return JsonOut;
				}
				else {
					var error = JsonConvert.DeserializeObject<APIError>(JsonOut);
					throw new Exception(error.message); 
				}
			}
			catch (Exception e) 
			{
				if (e.GetType() == typeof(HttpRequestException))
				{
					return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
				}
				return e.Message;
			}
		}

		public static async Task<Card[]> GetWallet() 
		{ 
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}wallet");
				string JsonOut = await result.Content.ReadAsStringAsync();
				Card[] cards = JsonConvert.DeserializeObject<Card[]>(JsonOut);
				return cards;
			}
			catch (Exception e) {
				return null;
			}
		}

		public static async Task<string> DeleteCard(string CardId) 
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.DeleteAsync($"{GlobalResources.RestAPIUrl}wallet/{CardId}");
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (JsonOut == "true")
				{
					return JsonOut;
				}
				else 
				{
					var error = JsonConvert.DeserializeObject<APIError>(JsonOut);
					throw new Exception(error.message);
				}
			}
			catch (Exception e) 
			{
				if (e.GetType() == typeof(HttpRequestException))
				{
					return "An Http Request Exception has been called.  This may be due to problems with your network.  Please check your connection and try again";
				}
				return e.Message;
			}
		}

		public static async Task<string> AddCard(StripeContainer token) 
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				var JsonIn = JsonConvert.SerializeObject(token);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}wallet", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (JsonOut.Contains("code")) {
					var error = JsonConvert.DeserializeObject<APIError>(JsonOut);
					throw new Exception($"Error: {error.message}");
				}
				return JsonOut;
			}
			catch (Exception e) 
			{
				if (e.GetType() == typeof(HttpRequestException))
				{
					return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
				}
				return e.Message;
			}
		}

		public static async Task<Donation[]> GetDonations() 
		{ 
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}donations");
				string JsonOut = await result.Content.ReadAsStringAsync();
				Donation[] donations = JsonConvert.DeserializeObject<Donation[]>(JsonOut);
				return donations;
			}
			catch (Exception e) {
				return null;
			}
		}

		public static async Task<string> UpdateDonation(putDonation donation) 
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				var JsonIn = JsonConvert.SerializeObject(donation);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.PutAsync($"{GlobalResources.RestAPIUrl}donations", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (JsonOut != "true") {
					APIError error = JsonConvert.DeserializeObject<APIError>(JsonOut);
					throw new Exception(error.message);
				}
				return "Success";
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		public static async Task<string> AddDonation(postDonation donation)
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				var JsonIn = JsonConvert.SerializeObject(donation);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}donations", content);
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (JsonOut != "true")	
				{
					APIError error = JsonConvert.DeserializeObject<APIError>(JsonOut);
					throw new Exception(error.message);
				}
				return "Success";
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		public static async Task<string> DeleteDonation(int id) 
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.DeleteAsync($"{GlobalResources.RestAPIUrl}donations/{id}");
				string JsonOut = await result.Content.ReadAsStringAsync();
				if (JsonOut != "true")
				{
					APIError error = JsonConvert.DeserializeObject<APIError>(JsonOut);
					throw new Exception(error.message);
				}
				return JsonOut;
			}
			catch (Exception e) {
				return e.Message;
			}
		}

		public static async Task<DonationRecord[]> GetDonationHistory()
		{
			try
			{
				dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}donations/history");
				string JsonOut = await result.Content.ReadAsStringAsync();
				DonationRecord[] history = JsonConvert.DeserializeObject<DonationRecord[]>(JsonOut);
				return history;
			}
			catch (Exception e)
			{
				return null;
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

		public static async Task CreateNewActionLog(int episodeId, string actionType, double playTime, bool? favorite = null) 
		{
			var actionLog = new dbPlayerActions();
			actionLog.ActionDateTime = DateTimeOffset.Now.LocalDateTime;
			actionLog.entity_type = favorite.HasValue? "favorite" : "episode";
			actionLog.EpisodeId = episodeId;
			actionLog.PlayerTime = playTime;
			actionLog.ActionType = actionType;
			actionLog.Favorite = favorite.HasValue ? favorite.Value : db.Table<dbEpisodes>().Single(x => x.id == episodeId).is_favorite;
			var user = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
			if (user != null) {
				actionLog.UserEmail = user.Value;
			}
			await adb.InsertAsync(actionLog);
		}

		public static async Task<string> PostActionLogs() {
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
					var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/logevents", content);
					string JsonOut = await result.Content.ReadAsStringAsync();
					if (JsonOut != "1")
					{
						throw new Exception();
					}
					foreach (var action in actions)
					{
						await adb.DeleteAsync(action);
					}
				}
				catch (Exception e) 
				{
					//It's bad if the program lands here.
					Debug.WriteLine($"Error in Posting Action logs: {e.Message}");

					return e.Message;
				}
			}
			return "OK";
		}

		public static async Task<bool> GetMemberData(){
			var start = DateTime.Now;
			var settings = await adb.Table<dbSettings>().ToListAsync();
			dbSettings TokenSettings = settings.Single(x => x.Key == "Token");
			dbSettings EmailSettings = settings.Single(x => x.Key == "Email");
			Debug.WriteLine($"Read data {(DateTime.Now - start).TotalMilliseconds}");
			try
			{
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
				var JsonIn = JsonConvert.SerializeObject(EmailSettings.Value);
				var content = new StringContent(JsonIn);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}member/data");
				string JsonOut = await result.Content.ReadAsStringAsync();
				MemberData container = JsonConvert.DeserializeObject<MemberData>(JsonOut);
				Debug.WriteLine($"Got member data from auth API {(DateTime.Now - start).TotalMilliseconds}");
				if (container.code == "rest_forbidden")
				{
					throw new Exception();
				}
				else {
					await SaveMemberData(container.episodes);
					Debug.WriteLine($"Done Saving Member data {(DateTime.Now - start).TotalMilliseconds}");
				}
				return true;
			}
			catch (Exception e) {
				Debug.WriteLine($"Exception in GetMemberData: {e.Message}");

				return false;
			}
		}

		static async Task SaveMemberData(List<dbEpisodes> episodes) {
			var savedEps = await adb.Table<dbEpisodes>().ToListAsync();
			List<dbEpisodes> insert = new List<dbEpisodes>();
			List<dbEpisodes> update = new List<dbEpisodes>();
			var start = DateTime.Now;
			foreach (dbEpisodes episode in episodes) {
				var saved = savedEps.SingleOrDefault(x => x.id == episode.id);
				if (saved == null)
				{
					insert.Add(episode);
				}
				else {
					saved.stop_time = episode.stop_time;
					saved.is_favorite = episode.is_favorite;
					saved.is_listened_to = episode.is_listened_to;
					saved.has_journal = episode.has_journal;
					update.Add(saved);
				}
			}
			await adb.InsertAllAsync(insert);
			await adb.UpdateAllAsync(update);
			Debug.WriteLine($"Writing new episode data {(DateTime.Now - start).TotalMilliseconds}");
		}
	}
}
