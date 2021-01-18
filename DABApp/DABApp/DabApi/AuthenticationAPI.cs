using System;
using System.Collections.Generic;
using System.Data.Common;
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
            var guestUserData = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;
            guestUserData.Token = "";
            guestUserData.Email = "";
            guestUserData.FirstName = "Guest";
            guestUserData.LastName = "Guest";
            guestUserData.WpId = 0;
            guestUserData.Channel = "";
            guestUserData.Channels = "";
            guestUserData.Id = 0;
            guestUserData.NickName = "Guest";
            guestUserData.UserRegistered = new DateTime(DateTime.Now.Year, 1, 1);
            guestUserData.TokenCreation = DateTime.Now;
            guestUserData.ActionDate = DateTime.MinValue;
            guestUserData.ProgressDate = DateTime.MinValue;

            adb.InsertOrReplaceAsync(guestUserData);
        }


        public static async Task<APIAddresses> GetAddresses()//Gets billing and shipping addresses for donations
        {
            try
            {
                string TokenSettingsValue = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Token;
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettingsValue);
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

        public static List<dbCreditCards> GetWallet()//Gets user's saved credit cards.  Used for donations
        {
            try
            {
                List<dbCreditCards> cards = adb.Table<dbCreditCards>().ToListAsync().Result;

                return cards;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static List<dbUserCampaigns> GetDonations()//Gets all recurring user donations.  Not historical ones!
        {
            try
            {
                List<dbUserCampaigns> donations = adb.Table<dbUserCampaigns>().Where(x => x.Status != "deleted").ToListAsync().Result;
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
                string TokenSettingsValue = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Token;
                HttpClient client = new HttpClient();
                var JsonIn = JsonConvert.SerializeObject(donation);
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettingsValue);
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
                string TokenSettingsValue = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Token;
                HttpClient client = new HttpClient();
                var JsonIn = JsonConvert.SerializeObject(donation);
                var content = new StringContent(JsonIn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenSettingsValue);
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

        public static async Task<string> DeleteDonation(string id)//Deletes recurring donations
        {
            try
            {
                dbUserCampaigns donation = adb.Table<dbUserCampaigns>().Where(x => x.Id == id).FirstOrDefaultAsync().Result;
                donation.Status = "deleted";
                await adb.InsertOrReplaceAsync(donation);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static List<dbDonationHistory> GetDonationHistory()//Gets user donation history
        {
            try
            {
                List<dbDonationHistory> donations = adb.Table<dbDonationHistory>().ToListAsync().Result;

                return donations;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task CreateNewActionLog(int episodeId, ServiceActionsEnum actionType, double? playTime, bool? listened, bool? favorite = null, bool? hasEmptyJournal = false)
        {
            try//Creates new action log which keeps track of user location on episodes.
            {
                //build a basic action log
                var actionLog = new DABApp.dbPlayerActions();
                string email = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Email;
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
                        actionLog.ActionType = "entryDate";
                        actionLog.HasJournal = true;
                        break;
                    case ServiceActionsEnum.PositionChanged:
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

        public static bool GetTestMode()
        {
            string testmode = dbSettings.GetSetting("TestMode", "");
            if (testmode != "")
            {
                return Convert.ToBoolean(testmode);
            }
            else return false;
        }

        public static bool GetExperimentMode()
        {
            var experimentmode = adb.Table<dbSettings>().Where(x => x.Key == "ExperimentMode").FirstOrDefaultAsync().Result;
            if (experimentmode != null)
            {
                return Convert.ToBoolean(experimentmode.Value);
            }
            else return false;
        }

        public static void SetExternalMode(bool isTest)
        {
            adb.QueryAsync<dbEpisodes>("delete from dbEpisodes");
            adb.ExecuteAsync("delete from dbPlayerActions");
            adb.ExecuteAsync("delete from Badge");
            adb.ExecuteAsync("delete from dbUserBadgeProgress");
            adb.ExecuteAsync("delete from Channel");
            adb.ExecuteAsync("delete from dbEpisodeUserData");
            if (isTest)
            {
                dbSettings.StoreSetting("TestMode", GlobalResources.TestMode.ToString());
            }
            else
            {
                dbSettings.StoreSetting("ExperimentMode", GlobalResources.ExperimentMode.ToString());
            }
        }
    }
}
