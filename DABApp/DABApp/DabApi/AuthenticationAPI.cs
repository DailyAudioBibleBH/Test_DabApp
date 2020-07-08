using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DABApp;
using DABApp.DabSockets;
using DABApp.Service;
using Newtonsoft.Json;
using Plugin.Connectivity;
using SQLite;
using Xamarin.Forms;
using static DABApp.Service.DabService;

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

        public static async Task CreateNewActionLog(int episodeId,ServiceActionsEnum actionType, double? playTime, bool? listened, bool? favorite = null, bool? hasEmptyJournal = false)
        {
            try//Creates new action log which keeps track of user location on episodes.
            {
                //TODO: only do this if logged in.

                //build a basic action log
                var actionLog = new DABApp.dbPlayerActions();
                string email = dbSettings.GetSetting("Email", "");
                actionLog.ActionDateTime = DateTimeOffset.Now.LocalDateTime;
                actionLog.EpisodeId = episodeId;
                actionLog.UserEmail = email;


                switch (actionType)
                {
                    case ServiceActionsEnum.Listened:
                        actionLog.ActionType = "listened_status";
                        actionLog.Listened = listened.Value;
                        break;
                    case ServiceActionsEnum.Favorite:
                        actionLog.ActionType = "favorite";
                        actionLog.Favorite = favorite.Value;
                        break;
                    case ServiceActionsEnum.Journaled:
                        //TODO: Fix this
                        throw new NotSupportedException("journal not working yet");
                    case ServiceActionsEnum.PositionChanged:
                        //TODO: Confirm this is the right code.
                        actionLog.ActionType = "pause";
                        actionLog.PlayerTime = playTime.Value;
                        break;
                }

                //delete existing action logs with same episode and type
                var oldActions = await adb.Table<dbPlayerActions>().Where(x => x.ActionType == actionLog.ActionType && x.EpisodeId == actionLog.EpisodeId && x.UserEmail == email).ToListAsync();
                foreach (var oldAction in oldActions)
                {
                    await adb.DeleteAsync(oldAction);
                }

                //insert new action log
                await adb.InsertAsync(actionLog);

                //send actions logs if we can
                await DabServiceRoutines.PostActionLogs();

            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception caught in AuthenticationAPI.CreateNewActionLog(): {e.Message}");
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
