using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.WebSocketHelper;
using Newtonsoft.Json;
using Plugin.Connectivity;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
    public class AuthenticationAPI
    {
        static SQLiteConnection db = DabData.database;
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

        static bool notPosting = true;
        static bool notGetting = true;

        public static async Task<string> ValidateLogin(string email, string password, bool IsGuest = false)//Asyncronously logs the user in used if the user is logging in as a guest as well.
        {
            try
            {
                dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
                dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
                dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
                dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
                dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
                if (IsGuest)//Setting database settings for guest login
                {
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
                    else
                    {
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
                    GuestLogin();
                    return "IsGuest";
                }
                else
                {
                    HttpClient client = new HttpClient();//Getting all login user info from the Authentication API
                    var JsonIn = JsonConvert.SerializeObject(new LoginInfo(email, password));
                    var content = new StringContent(JsonIn);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member", content);
                    //if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new HttpRequestException(result.ReasonPhrase);
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
                    else//Setting database settings for user based on what is returned by the Authentication API.
                    {
                        if (EmailSettings.Value != email) GuestLogin();
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
                    //TODO: Replacew this with sync
                    //JournalTracker.Current.Connect(token.value);
                    if (!string.IsNullOrEmpty(token.user_avatar)) GuestStatus.Current.AvatarUrl = token.user_avatar;
                    return "Success";
                }
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(HttpRequestException))
                {
                    if (!CrossConnectivity.Current.IsConnected)
                    {
                        return $"Your device is currently not connected to the internet.  If you would like to continue please log in as a guest and log in when you have an internet connection.";
                    }
                    else return $"An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again.  Exception: {e.Message}";
                }
                else return e.Message;
            }
        }

        public static bool CheckToken(int days = 0)//Checking API given token which determines if user needs to log back in after a set amount of time.
        {
            var expiration = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");

            if (expiration == null)
            {
                return false;
            }
            if (expiration.Value == null) return false;
            DateTime expirationDate = DateTime.Parse(expiration.Value);
            if (expirationDate <= DateTime.Now.AddDays(days))
            {
                return false;
            }
            //TODO: Replacew this with sync
            //var token = db.Table<dbSettings>().Single(x => x.Key == "Token");
            //if (!JournalTracker.Current.IsConnected && CrossConnectivity.Current.IsConnected)
            //{
            //    JournalTracker.Current.Connect(token.Value);
            //}
            return true;
        }

        //TODO: Replacew this with sync
        //public static void ConnectJournal()//Connecting Journal Tracker when user logs in.  Done here because of access to the database Token setting.
        //{
        //    try
        //    {
        //        if (!JournalTracker.Current.IsConnected && CrossConnectivity.Current.IsConnected)
        //        {
        //            var token = db.Table<dbSettings>().Single(x => x.Key == "Token");
        //            JournalTracker.Current.Connect(token.Value);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.WriteLine($"Exception caught in AuthenticationAPI.ConnectJournal(): {e.Message}");
        //    }
        //}

        public static async Task<string> CreateNewMember(string firstName, string lastName, string email, string password)//Creates a new member.
        {
            try
            {
                dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                dbSettings ExpirationSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
                dbSettings EmailSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
                dbSettings FirstNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
                dbSettings LastNameSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
                dbSettings AvatarSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
                HttpClient client = new HttpClient();//Authentication Bearer token is hard coded in GlobalResources. 
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalResources.APIKey);
                var JsonIn = JsonConvert.SerializeObject(new SignUpInfo(email, firstName, lastName, password));
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/profile", content);
                string JsonOut = await result.Content.ReadAsStringAsync();
                APITokenContainer container = JsonConvert.DeserializeObject<APITokenContainer>(JsonOut);
                APIToken token = container.token;
                if (container.code == "rest_forbidden" || container.code == "add_member_error")
                {
                    return "An error occured: " + container.message;
                }
                if (TokenSettings == null)//If the AuthenticationAPI does not have the new member data then creae it.  Otherwise login normally.
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
                    IEnumerable<dbSettings> settings = new dbSettings[] { TokenSettings, ExpirationSettings, EmailSettings, FirstNameSettings, LastNameSettings, AvatarSettings };
                    await adb.UpdateAllAsync(settings);
                    //GuestStatus.Current.AvatarUrl = new Uri(token.user_avatar);
                    GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
                }
                //TODO: Replacew this with sync
                //JournalTracker.Current.Connect(TokenSettings.Value);
                return "";
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(HttpRequestException))
                {
                    return "Http Request Timed out.";
                }
                else return "The following exception was caught: " + e.Message;
            }
        }

        public static async Task<string> ResetPassword(string email)//Sends reset email request to API which then takes care of the rest.
        {
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
            catch (Exception e)
            {
                return "The following exception was caught: " + e.Message;
            }
        }

        public static async Task<bool> LogOut()//Logs the user out.
        {
            try
            {
                dbSettings TokenSettings = db.Table<dbSettings>().Single(x => x.Key == "Token");
                dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalResources.APIKey);
                var JsonIn = JsonConvert.SerializeObject(new LogOutInfo(TokenSettings.Value));
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/logout", content);
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Error Logging Out: {result.StatusCode}");
                }
                ExpirationSettings.Value = DateTime.MinValue.ToString();
                await adb.UpdateAsync(ExpirationSettings);
                return true;
            }
            catch (Exception e)
            {
                dbSettings ExpirationSettings = db.Table<dbSettings>().Single(x => x.Key == "TokenExpiration");
                ExpirationSettings.Value = DateTime.MinValue.ToString();
                await adb.UpdateAsync(ExpirationSettings);
                return false;
            }
        }

        public static async Task<bool> ExchangeToken()//Gets new token from the API App uses this whenever user arrives onto channels page and the current token is expired.
        {
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
                if (container.token == null)
                {
                    throw new Exception($"Error Exchanging Token: {container.message}");
                }
                TokenSettings.Value = token.value;
                ExpirationSettings.Value = token.expires;
                await adb.UpdateAsync(TokenSettings);
                await adb.UpdateAsync(ExpirationSettings);
                //TODO: Replace this with sync
                //JournalTracker.Current.Connect(token.value);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static async Task<bool> GetMember()//Used to get user profile info for the DabSettingsPage.  Also gets the current user settings from the API and updates the App user settings.
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
                    throw new Exception($"Error Getting Member: email is null");
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
            try//Edits member data used on DABProfileManagementPage
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
                var result = await client.PutAsync($"{GlobalResources.RestAPIUrl}member/profile", content);//Using an HttpPut method to update member profle
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
                await adb.UpdateAsync(TokenSettings);//Updating settings only if the API gets successfully updated.
                await adb.UpdateAsync(ExpirationSettings);
                await adb.UpdateAsync(EmailSettings);
                await adb.UpdateAsync(FirstNameSettings);
                await adb.UpdateAsync(LastNameSettings);
                return "Success";
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

        public static async Task<APIAddresses> GetAddresses()//Gets billing and shipping addresses for donations
        {
            try
            {
                dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                var result = await client.GetAsync($"{GlobalResources.RestAPIUrl}addresses");
                string JsonOut = await result.Content.ReadAsStringAsync();
                APIAddresses addresses = JsonConvert.DeserializeObject<APIAddresses>(JsonOut);
                if (addresses.billing == null)
                {
                    throw new Exception($"Error getting billing address");
                }
                return addresses;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<Country[]> GetCountries()//Gets countries array for updating user addresses so that doesn't have to be hard coded.
        {
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
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<string> UpdateBillingAddress(Address newBilling)//Updating the Billing Address 
        {
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
                    return "An Http Request Exception has been called this may be due to problems with your network.  Please check your connection and try again";
                }
                return e.Message;
            }
        }

        public static async Task<Card[]> GetWallet()//Gets user's saved credit cards.  Used for donations
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
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<string> DeleteCard(string CardId)//Deletes user credit card from user wallet
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

        public static async Task<string> AddCard(StripeContainer token)//Adds user credit card using the Stripe Xamarin API
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
                Debug.WriteLine($"Wallet Error: {JsonOut}");
                if (JsonOut.Contains("code"))
                {
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

        public static async Task<Donation[]> GetDonations()//Gets all recurring user donations.  Not historical ones!
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
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<string> UpdateDonation(putDonation donation)//Updates a preexisting donation
        {
            try
            {
                dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                HttpClient client = new HttpClient();
                var JsonIn = JsonConvert.SerializeObject(donation);
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                var result = await client.PutAsync($"{GlobalResources.RestAPIUrl}donations", content);//Uses HttpPut method to update donation
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

        public static async Task<string> AddDonation(postDonation donation)//Adding a donation. Either one time or recurring donation
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

        public static async Task<string> DeleteDonation(int id)//Deletes recurring donations
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
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static async Task<DonationRecord[]> GetDonationHistory()//Gets user donation history
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

        static void CreateSettings(APIToken token)//Class which creates new user settings
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

        public static async Task CreateNewActionLog(int episodeId, string actionType, double playTime, string listened, bool? favorite = null)
        {
            try//Creates new action log which keeps track of user location on episodes.
            {
                var actionLog = new dbPlayerActions();
                actionLog.ActionDateTime = DateTimeOffset.Now.LocalDateTime;
                var entity_type = actionType == "listened" ? "listened_status" : "episode";
                actionLog.entity_type = favorite.HasValue ? "favorite" : entity_type;
                actionLog.EpisodeId = episodeId;
                actionLog.PlayerTime = playTime;
                actionLog.ActionType = actionType;
                actionLog.Favorite = favorite.HasValue ? favorite.Value : db.Table<dbEpisodes>().Single(x => x.id == episodeId).is_favorite;
                actionLog.listened_status = actionType == "listened" ? listened : db.Table<dbEpisodes>().Single(x => x.id == episodeId).is_listened_to;
                var user = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
                if (user != null)
                {
                    actionLog.UserEmail = user.Value;
                }
                if (Device.RuntimePlatform == "Android")
                {
                    db.Insert(actionLog);
                }
                else await adb.InsertAsync(actionLog);
            }
            catch (Exception e)
            {
                HockeyApp.MetricsManager.TrackEvent($"Exception caught in AuthenticationAPI.CreateNewActionLog(): {e.Message}");
                Debug.WriteLine($"Exception caught in AuthenticationAPI.CreateNewActionLog(): {e.Message}");
            }
        }

        public static async Task<string> PostActionLogs()//Posts action logs to API in order to keep user episode location on multiple devices.
        {
            //TODO: Replace this with sync


            if (!GuestStatus.Current.IsGuestLogin && DabSyncService.Instance.IsConnected)
            {

                if (notPosting)
                {
                    string listenedTo;
                    notPosting = false;
                    dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                    var actions = db.Table<dbPlayerActions>().ToList();
                    if (TokenSettings != null && actions.Count > 0) 
                    {
                        try
                        {
                            LoggedEvents events = new LoggedEvents();
                            foreach (var i in actions)
                            {
                                if (i.listened_status == "listened")
                                {
                                    listenedTo = "true";
                                }
                                else
                                {
                                    listenedTo = "false";
                                }
                                var variables = new Variables();
                                var query = "mutation {\n            logAction(episodeId: " + i.EpisodeId + ", listen: " + listenedTo + ") {\n                episodeId\n                listen\n                position\n                favorite\n                entryDate\n            }\n        }";
                                var payload = new WebSocketHelper.Payload(query, variables);
                                var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", payload));
                                
                                DabSyncService.Instance.Send(JsonIn);
                            }
                            //HttpClient client = new HttpClient();
                            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                            //events.data = PlayerEpisodeAction.ParsePlayerActions(actions);
                            //var JsonIn = JsonConvert.SerializeObject(events);
                            //var content = new StringContent(JsonIn);
                            //content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                            //var result = await client.PostAsync($"{GlobalResources.RestAPIUrl}member/logevents", content);
                            //string JsonOut = await result.Content.ReadAsStringAsync();
                            //if (JsonOut != "1")
                            //{
                            //    throw new Exception(JsonOut);
                            //}
                            //foreach (var action in actions)
                            //{
                            //    await adb.DeleteAsync(action);
                            //}
                        }
                        catch (Exception e)
                        {
                            //It's bad if the program lands here.
                            Debug.WriteLine($"Error in Posting Action logs: {e.Message}");
                            notPosting = true;
                            return e.Message;
                        }
                    }
                    notPosting = true;
                    return "OK";
                }
                return "Currently Posting Action Logs";
            }
            else
            {
                return "Skipping Action Log Posts for Guest...";
            }
        }

        public static async Task<bool> GetMemberData()//Getting member info on episodes.  So that user location on episodes is updated.
        {
            if (!GuestStatus.Current.IsGuestLogin)
            //TODO: Journal?
            //if (!GuestStatus.Current.IsGuestLogin && JournalTracker.Current.Open)
            {
                if (notGetting)
                {

                    notGetting = false;
                    var start = DateTime.Now;
                    var settings = await adb.Table<dbSettings>().ToListAsync();
                    dbSettings TokenSettings = settings.SingleOrDefault(x => x.Key == "Token");
                    dbSettings EmailSettings = settings.SingleOrDefault(x => x.Key == "Email");
                    if (TokenSettings != null || EmailSettings != null)
                    {
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
                            if (container.code == "rest_forbidden")
                            {
                                throw new Exception($"server returned following error code:{container.code} with message: {container.message}");
                            }
                            else
                            {
                                // Clean up null EpisodeIds
                                Debug.WriteLine($"Pre Cleanup Episode Count: {container.episodes.Count()}");
                                container.episodes = container.episodes.Where(x => x.id != null).ToList(); //Get rid of null episodes
                                Debug.WriteLine($"Post Cleanup Episode Count: {container.episodes.Count()}");
                                Debug.WriteLine($"Got member data from auth API {(DateTime.Now - start).TotalMilliseconds}");
                                //Save member data
                                await SaveMemberData(container.episodes);//Saving member data to SQLite database.
                                Debug.WriteLine($"Done Saving Member data {(DateTime.Now - start).TotalMilliseconds}");
                            }
                            notGetting = true;
                            return true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"Exception in GetMemberData: {e.Message}");
                            notGetting = true;
                            return false;
                        }
                    }
                    notGetting = true;
                    return false;
                }
                //Already Getting
                return false;
            }
            else
            {
                //Not logged in
                Debug.WriteLine("Skipping GetMemberData for guest login...");
                return false;
            }
        }

        static async Task SaveMemberData(List<dbEpisodes> episodes)//Saves member data for each episode gotten from GetMemberData method.
        {
            var savedEps = await adb.Table<dbEpisodes>().ToListAsync();
            //List<dbEpisodes> insert = new List<dbEpisodes>();
            List<dbEpisodes> update = new List<dbEpisodes>();
            var start = DateTime.Now;
            var potential = savedEps.Where(x => x.is_favorite == true || x.is_listened_to == "listened").ToList();
            foreach (dbEpisodes p in potential)
            {
                if (!episodes.Any(x => x.id == p.id))
                {
                    p.is_favorite = false;
                    p.is_listened_to = "";
                    update.Add(p);
                }
            }
            foreach (dbEpisodes episode in episodes)
            {
                var saved = savedEps.SingleOrDefault(x => x.id == episode.id);
                //if (saved == null)
                //{
                //    insert.Add(episode);
                //}
                //else
                //{
                if (saved != null)
                {
                    if (!(saved.stop_time == episode.stop_time && saved.is_favorite == episode.is_favorite && saved.is_listened_to == episode.is_listened_to && saved.has_journal == episode.has_journal))
                    {
                        saved.stop_time = episode.stop_time;
                        saved.is_favorite = episode.is_favorite;
                        saved.is_listened_to = episode.is_listened_to;
                        saved.has_journal = episode.has_journal;
                        update.Add(saved);
                    }
                }
            }
            //await adb.InsertAllAsync(insert);
            await adb.UpdateAllAsync(update);
            Debug.WriteLine($"Writing new episode data {(DateTime.Now - start).TotalMilliseconds}");
        }

        static void GuestLogin()//Deletes all user episode data when a guest logs in.
        {
            var episodes = db.Table<dbEpisodes>();
            if (episodes.Count() > 0)
            {
                db.DeleteAll<dbEpisodes>();
            }
        }

        public static bool GetTestMode()
        {
            var testmode = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TestMode");
            if (testmode != null)
            {
                return Convert.ToBoolean(testmode.Value);
            }
            else return false;
        }

        public static void SetTestMode()
        {
            var testMode = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TestMode");
            dbSettings newMode = new dbSettings();
            db.Query<dbEpisodes>("delete from dbEpisodes");
            newMode.Key = "TestMode";
            newMode.Value = GlobalResources.TestMode.ToString();
            if (testMode != null)
            {
                db.Update(newMode);
            }
            else db.InsertOrReplace(newMode);
        }

        public static string CurrentToken
        //Return the current token
        {
            get
            {
                dbSettings TokenSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                return TokenSettings?.Value;
            }
        }
    }
}
