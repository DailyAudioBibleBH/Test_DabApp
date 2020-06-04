using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DABApp;
using DABApp.DabSockets;
using Newtonsoft.Json;
using Plugin.Connectivity;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
    public class AuthenticationAPI
    {
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

        static DabGraphQlVariables variables = new DabGraphQlVariables(); //Instance used for websocket communication


        static bool notPosting = true;
        static bool notGetting = true;
        static bool favorite; 

        public static async Task<string> ValidateLogin(string email, string password, bool IsGuest = false)//Asyncronously logs the user in used if the user is logging in as a guest as well.
        {
            try
            {
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                dbSettings CreationSettings = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
                dbSettings AvatarSettings = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
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
                        if (TokenSettings != null) TokenSettings.Value = "";
                        if (EmailSettings != null) EmailSettings.Value = "Guest";
                        if (CreationSettings != null) CreationSettings.Value = DateTime.Now.ToString();
                        if (FirstNameSettings != null) FirstNameSettings.Value = "";
                        if (LastNameSettings != null) LastNameSettings.Value = "";
                        if (AvatarSettings != null) AvatarSettings.Value = "";
                        IEnumerable<dbSettings> settings = Enumerable.Empty<dbSettings>();
                        settings = new dbSettings[] { TokenSettings, CreationSettings, EmailSettings, FirstNameSettings, LastNameSettings, AvatarSettings };
                        await adb.UpdateAllAsync(settings);
                    }
                    GuestLogin();
                    return "IsGuest";
                }
                else
                {
                    //Initiate log in with web socket
                    /*
                      mutation {
                      loginUser(email: "djtest@lutd.io", password: "asdfasdf", version: 1) {token}}
                     */

                    string jLogin = $"mutation {{loginUser(email: \"{email}\", password: \"{password}\", version: 1) {{token}}}}";
                    var pLogin = new DabGraphQlPayload(jLogin, variables);
                    DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", pLogin)));
                    return "Request Sent"; //Let the caller know we're waitin gfor a reply from GraphQL.
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

        public static bool CheckTokenOnAppStart()//Checking API given token which determines if user can be brought to channels page on login
        {
            try
            {

                var creation = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                int days = ContentConfig.Instance.options.token_life;
                if (creation == null || creation.Value == null)
                {
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public static bool CheckToken()//Checking API given token which determines if user needs to log back in after a set amount of time.
        {
            try
            {

            var creation = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result ;
            int days = ContentConfig.Instance.options.token_life;
            if (creation == null || creation.Value == null)
            {
                return false;
            }
            DateTime creationDate = DateTime.Parse(creation.Value);
            if (DateTime.Now > creationDate.AddDays(days))
            {
                return false;
            }

            return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public static async Task<string> CreateNewMember(string firstName, string lastName, string email, string password)//Creates a new member.
        {
            try
            {
                //TODO: Move to DabSyncService
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                dbSettings CreationSettings = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
                dbSettings AvatarSettings = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
                HttpClient client = new HttpClient();//Authentication Bearer token is hard coded in GlobalResources. 
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalResources.APIKey);
                var JsonIn = JsonConvert.SerializeObject(new SignUpInfo(email, firstName, lastName, password));
                var content = new StringContent(JsonIn);

                string registerMutation = $"mutation {{registerUser(email: \"{email}\", firstName: \"{firstName}\", lastName: \"{lastName}\", password: \"{password}\"){{ id wpId firstName lastName nickname email language channel channels userRegistered token }}";
                var mRegister = new DabGraphQlPayload(registerMutation, variables);
                DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", mRegister)));


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
                    CreationSettings.Value = token.expires;
                    EmailSettings.Value = token.user_email;
                    FirstNameSettings.Value = token.user_first_name;
                    LastNameSettings.Value = token.user_last_name;
                    AvatarSettings.Value = token.user_avatar;
                    IEnumerable<dbSettings> settings = new dbSettings[] { TokenSettings, CreationSettings, EmailSettings, FirstNameSettings, LastNameSettings, AvatarSettings };
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

        public static async Task<bool> GetMember()//Used to get user profile info for the DabSettingsPage.  Also gets the current user settings from the API and updates the App user settings.
        {
            try
            {
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
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

        public static async Task<string> EditMember(string email, string firstName, string lastName)
        {
            try//Edits member data used on DABProfileManagementPage
            {
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                dbSettings CreationSettings = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettings.Value);
                var JsonIn = JsonConvert.SerializeObject(new EditProfileInfo(email, firstName, lastName));
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
                CreationSettings.Value = token.expires;
                EmailSettings.Value = token.user_email;
                FirstNameSettings.Value = token.user_first_name;
                LastNameSettings.Value = token.user_last_name;
                await adb.UpdateAsync(TokenSettings);//Updating settings only if the API gets successfully updated.
                await adb.UpdateAsync(CreationSettings);
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
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
            var CreationSettings = new dbSettings();
            CreationSettings.Key = "TokenCreation";
            CreationSettings.Value = token.expires;
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
            int x = adb.InsertOrReplaceAsync(TokenSettings).Result;
            x= adb.InsertOrReplaceAsync(CreationSettings).Result;
            x = adb.InsertOrReplaceAsync(EmailSettings).Result;
            x = adb.InsertOrReplaceAsync(FirstNameSettings).Result;
            x = adb.InsertOrReplaceAsync(LastNameSettings).Result;
            x = adb.InsertOrReplaceAsync(AvatarSettings).Result;

            dbSettings.StoreSetting("Language", "English");
            dbSettings.StoreSetting("Channel", "");
            dbSettings.StoreSetting("Channels", "");
            dbSettings.StoreSetting("NickName", "");

            GuestStatus.Current.UserName = $"{token.user_first_name} {token.user_last_name}";
        }

        public static async Task CreateNewActionLog(int episodeId, string actionType, double? playTime, bool? listened, bool? favorite = null, bool? hasEmptyJournal = false)
        {
            try//Creates new action log which keeps track of user location on episodes.
            {
                var actionLog = new DABApp.dbPlayerActions();
                actionLog.ActionDateTime = DateTimeOffset.Now.LocalDateTime;
                var entity_type = actionType == "listened" ? "listened_status" : "episode";
                actionLog.entity_type = favorite.HasValue ? "favorite" : entity_type;
                actionLog.EpisodeId = episodeId;
                actionLog.PlayerTime = playTime.HasValue ? playTime.Value : adb.Table<dbEpisodes>().Where(x => x.id == episodeId).FirstAsync().Result.UserData.CurrentPosition;
                actionLog.ActionType = actionType;
                actionLog.Favorite = favorite.HasValue ? favorite.Value : adb.Table<dbEpisodes>().Where(x => x.id == episodeId).FirstAsync().Result.UserData.IsFavorite;
                //check this
                actionLog.listened_status = actionType == "listened" ? listened.ToString() : adb.Table<dbEpisodes>().Where(x => x.id == episodeId).FirstAsync().Result.UserData.IsListenedTo.ToString();
                var user = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                if (user != null)
                {
                    actionLog.UserEmail = user.Value;
                }

                //Android - Delete all existing action logs for this episode.
                var actionList = adb.Table<dbPlayerActions>().ToListAsync().Result;

                foreach (var i in actionList)
                {
                    if (i.EpisodeId == episodeId && i.ActionType == actionType)
                    {
                        await adb.DeleteAsync(i);
                    }
                }

                //Insert new action log
                if (Device.RuntimePlatform == "Android")
                {

                    //Android - add nw

                    await adb.InsertAsync(actionLog);
                }

                else
                {//Apple - asnc
                        //Add new episode action log
                    await adb.InsertAsync(actionLog);
                }
                if (hasEmptyJournal == null)
                {
                    hasEmptyJournal = false;
                }
                await PostActionLogs((bool)hasEmptyJournal);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception caught in AuthenticationAPI.CreateNewActionLog(): {e.Message}");
            }
        }

        public static async Task<string> PostActionLogs(bool hasEmptyJournal)//Posts action logs to API in order to keep user episode location on multiple devices.
        {
            if (!GuestStatus.Current.IsGuestLogin && DabSyncService.Instance.IsConnected)
            {
                if (notPosting)
                {
                    string listenedTo;
                    notPosting = false;
                    dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                    var actions = adb.Table<dbPlayerActions>().ToListAsync().Result;
                    
                    if (TokenSettings != null && actions.Count > 0) 
                    {
                        try
                        {
                            LoggedEvents events = new LoggedEvents();
                           
                            foreach (var i in actions)
                            {
                                var updatedAt = DateTime.UtcNow.ToString("o");
                                
                                switch (i.ActionType)
                                {
                                    case "favorite": //Favorited an episode mutation
                                        var favQuery = "mutation {logAction(episodeId: " + i.EpisodeId + ", favorite: " + i.Favorite + ", updatedAt: \"" + updatedAt + "\") {episodeId userId favorite updatedAt}}";
                                        favQuery = favQuery.Replace("True", "true");
                                        favQuery = favQuery.Replace("False", "false"); //Capitolized when converted to string so we undo this
                                        var favPayload = new DabGraphQlPayload(favQuery, variables);
                                        var favJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", favPayload));

                                        DabSyncService.Instance.Send(favJsonIn);
                                        //await PlayerFeedAPI.UpdateEpisodeProperty(i.EpisodeId, null, true, null, null);
                                        break;
                                    case "listened": //Marked as listened mutation
                                        if (i.listened_status == "True" || i.listened_status == "listened")
                                            listenedTo = "true";
                                        else
                                            listenedTo = "false";

                                        var lisQuery = "mutation {logAction(episodeId: " + i.EpisodeId + ", listen: " + listenedTo + ", updatedAt: \"" + updatedAt + "\") {episodeId userId listen updatedAt}}";
                                        var lisPayload = new DabGraphQlPayload(lisQuery, variables);
                                        var lisJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", lisPayload));

                                        DabSyncService.Instance.Send(lisJsonIn);
                                        //await PlayerFeedAPI.UpdateEpisodeProperty(i.EpisodeId, true, null, null, null);
                                        break;
                                    case "pause": //Saving player position to socket on pause mutation
                                        var posQuery = "mutation {logAction(episodeId: " + i.EpisodeId + ", position: " + (int)i.PlayerTime + ", updatedAt: \"" + updatedAt + "\") {episodeId userId position updatedAt}}";
                                        var posPayload = new DabGraphQlPayload(posQuery, variables);
                                        var posJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", posPayload));

                                        DabSyncService.Instance.Send(posJsonIn);
                                        break;
                                    case "entryDate": //When event happened mutation
                                        string entryDate = DateTime.Now.ToString("yyyy-MM-dd");
                                        var entQuery = "mutation {logAction(episodeId: " + i.EpisodeId + ", entryDate: \"" + entryDate + "\", updatedAt: \"" + updatedAt + "\") {episodeId userId entryDate updatedAt}}";
                                        if (hasEmptyJournal == true)
                                            entQuery = "mutation {logAction(episodeId: " + i.EpisodeId + ", entryDate: null , updatedAt: \"" + updatedAt + "\") {episodeId userId entryDate updatedAt}}";
                                        var entPayload = new DabGraphQlPayload(entQuery, variables);
                                        var entJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", entPayload));

                                        DabSyncService.Instance.Send(entJsonIn);
                                        break;
                                    default:
                                        break;
                                }
                            }
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
            if (!GuestStatus.Current.IsGuestLogin && DabSyncService.Instance.IsConnected)
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
                            if (GlobalResources.GetUserEmail() != "Guest")
                            {
                                //Send last action query to the websocket
                                Debug.WriteLine($"Getting actions since {GlobalResources.LastActionDate.ToString()}...");
                                var updateEpisodesQuery = "{ lastActions(date: \"" + GlobalResources.LastActionDate.ToString("o") + "Z\") { edges { id episodeId userId favorite listen position entryDate updatedAt createdAt } pageInfo { hasNextPage endCursor } } } ";
                                var updateEpisodesPayload = new DabGraphQlPayload(updateEpisodesQuery, variables);
                                var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", updateEpisodesPayload));
                                DabSyncService.Instance.Send(JsonIn);

                                notGetting = true;
                                return true;
                            }
                            else
                            {
                                notGetting = true;
                                return false;
                            }
                            
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
            var potential = savedEps.Where(x => x.UserData.IsFavorite == true || x.UserData.IsListenedTo == true).ToList();
            foreach (dbEpisodes p in potential)
            {
                if (!episodes.Any(x => x.id == p.id))
                {
                    p.UserData.IsFavorite = false;
                    p.UserData.IsListenedTo = false;
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
                    if (!(saved.UserData.CurrentPosition == episode.UserData.CurrentPosition && saved.UserData.IsFavorite == episode.UserData.IsFavorite && saved.UserData.IsListenedTo == episode.UserData.IsListenedTo && saved.UserData.HasJournal == episode.UserData.HasJournal))
                    {
                        saved.UserData.CurrentPosition = episode.UserData.CurrentPosition;
                        saved.UserData.IsFavorite = episode.UserData.IsFavorite;
                        saved.UserData.IsListenedTo = episode.UserData.IsListenedTo;
                        saved.UserData.HasJournal = episode.UserData.HasJournal;
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
            ////This is no longer needed because user data is kept separate from episodes
            //var episodes = db.Table<dbEpisodes>();
            //if (episodes.Count() > 0)
            //{
            //    db.DeleteAll<dbEpisodes>();
            //}
        }

        public static bool GetTestMode()
        {
            var testmode = adb.Table<dbSettings>().Where(x => x.Key == "TestMode").FirstOrDefaultAsync().Result;
            if (testmode != null)
            {
                return Convert.ToBoolean(testmode.Value);
            }
            else return false;
        }

        public static void SetTestMode()
        {
            var testMode = adb.Table<dbSettings>().Where(x => x.Key == "TestMode").FirstOrDefaultAsync().Result;
            dbSettings newMode = new dbSettings();
            adb.QueryAsync<dbEpisodes>("delete from dbEpisodes");
            adb.ExecuteAsync("delete from dbPlayerActions");
            adb.ExecuteAsync("delete from Badge");
            adb.ExecuteAsync("delete from dbUserBadgeProgress");
            adb.ExecuteAsync("delete from Channel");
            adb.ExecuteAsync("delete from dbEpisodeUserData");
            newMode.Key = "TestMode";
            newMode.Value = GlobalResources.TestMode.ToString();
            if (testMode != null)
            {
                var x = adb.UpdateAsync(newMode).Result;
            }
            else
            {
                var x = adb.InsertOrReplaceAsync(newMode).Result;
            }
        }

        public static string CurrentToken
        //Return the current token
        {
            get
            {
                dbSettings TokenSettings = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                return TokenSettings?.Value;
            }
        }
    }
}
