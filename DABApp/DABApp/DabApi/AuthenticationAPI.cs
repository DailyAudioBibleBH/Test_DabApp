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

        public static void LoginGuest()
        {
            dbSettings.StoreSetting("Token", "");
            dbSettings.StoreSetting("Email", "");
            dbSettings.StoreSetting("TokenCreation", "");
            dbSettings.StoreSetting("FirstName", "Guest");
            dbSettings.StoreSetting("LastName", "Guest");
            dbSettings.StoreSetting("Avatar", "");

        }

        public static bool IsTokenStillValid() 
        {
            /* this method checks to see if the user's token needs to be renewed
             */
            try
            {

                var creation = DateTime.Parse(dbSettings.GetSetting("TokenCreation", DateTime.MinValue.ToString()));
                int days = ContentConfig.Instance.options.token_life;
#if DEBUG
                days = -1; //always renew in debug mode
#endif
                if (DateTime.Now > creation.AddDays(days))
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
            x = adb.InsertOrReplaceAsync(CreationSettings).Result;
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

        static void CleanMemberData()
        {
            //Determine if this is the furst run of this version and if we need to do some cleanup           
            var version = Version.Plugin.CrossVersion.Current.Version;
            var firstRun = dbSettings.GetSetting($"FirstRun_{version}", "");
            if (firstRun == "")
            {

                //cleans up database for the first time for specific versions of the app
                var adb = DabData.AsyncDatabase;
                var a = version.Split(".");
                var shortVersion = a[0] + "." + a[1] + "." + a[2]; //xx.xx.xx format

                //reset action date for specific version of the app.
                List<string> VersionsToResetActionsOn = new List<string>() { "1.1.72", "1.1.73", "1.1.74", "1.1.80" };
                if (VersionsToResetActionsOn.Contains(shortVersion))
                {
                    //delete action dates
                    var dateSettings = adb.Table<dbSettings>().Where(x => x.Key.StartsWith("ActionDate-")).ToListAsync().Result;
                    foreach (var item in dateSettings)
                    {
                        int j = adb.DeleteAsync(item).Result;
                    }

                    //delete actions
                    int i = adb.ExecuteAsync("delete from dbPlayerActions").Result;
                    i = adb.ExecuteAsync("delete from dbEpisodeUserData").Result;
                }

                //store the setting that we have ran through this for the first run of the version
                dbSettings.StoreSetting($"FirstRun_{version}", DateTime.Now.ToString());

            }

        }

        public static async Task<bool> GetMemberData()//Getting member info on episodes.  So that user location on episodes is updated.
        {
            if (!GuestStatus.Current.IsGuestLogin && DabSyncService.Instance.IsConnected)
            {

                CleanMemberData(); //clean up member data if needed, normally only on the first time throuah an app.

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
            await adb.UpdateAllAsync(update);
            Debug.WriteLine($"Writing new episode data {(DateTime.Now - start).TotalMilliseconds}");
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
