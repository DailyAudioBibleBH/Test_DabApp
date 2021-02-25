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

        public static List<dbCreditCards> GetWallet()//Gets user's saved credit cards.  Used for donations
        {
            try
            {
                int wpid = GlobalResources.Instance.LoggedInUser.WpId;
                List<dbCreditCards> cards = adb.Table<dbCreditCards>().Where(x => x.cardUserId == wpid && (x.cardStatus == null || x.cardStatus != "deleted")).ToListAsync().Result;

                return cards;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static List<dbUserCampaigns> GetActiveDonations()//Gets all recurring user donations.  Not historical ones!
        {
            try
            {
                int wpid = GlobalResources.Instance.LoggedInUser.WpId;
                string del = "deleted";
                List<dbUserCampaigns> donations = adb.Table<dbUserCampaigns>().Where(x => x.UserWpId == wpid && x.Status != del).ToListAsync().Result;
                return donations;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static List<dbUserCampaigns> GetActiveDonationsByCamp(int campaignWpId)//Gets all recurring user donations.  Not historical ones!
        {
            try
            {
                string del = "deleted";
                List<dbUserCampaigns> donations = adb.Table<dbUserCampaigns>().Where(x => x.Status != del && x.CampaignWpId == campaignWpId).ToListAsync().Result;
                return donations;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static List<dbDonationHistory> GetDonationHistory()//Gets user donation history
        {
            try
            {
                int wpid = GlobalResources.Instance.LoggedInUser.WpId;

                List<dbDonationHistory> donations = adb.Table<dbDonationHistory>().Where(x => x.historyUserWpId == wpid).ToListAsync().Result;

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
                string email = GlobalResources.Instance.LoggedInUser.Email;
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
                string actType = actionLog.ActionType;
                int epId = actionLog.EpisodeId;
                var oldActions = await adb.Table<dbPlayerActions>().Where(x => x.ActionType == actType && x.EpisodeId == epId && x.UserEmail == email).ToListAsync();
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
