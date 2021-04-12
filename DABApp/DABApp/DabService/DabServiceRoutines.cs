using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.DabUI;
using DABApp.DabUI.BaseUI;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using static DABApp.Service.DabService;

namespace DABApp.Service
{
    public static class DabServiceRoutines
    {
        /* 
         * This class is focused on common routines used by DabService throughout the app that involve more than just querying the service. 
         * Methods in this class may interact with the database, take UI elements as arguments to update, or send messages.
         * It is important to leave DabService class focused on GraphQL interaction only.
         */

        #region Connection Routines

        public static async Task<bool> RunConnectionEstablishedRoutines()
        {
            /*
             * runs common routines after we estabish a connection
             */

            try
            {
                //common routines (logged in or not)
                var adb = DabData.AsyncDatabase;

                // get channels
                var ql = await DabService.GetChannels();
                if (ql.Success)
                {
                    foreach (var c in ql.Data.payload.data.channels)
                    {
                        await adb.InsertOrReplaceAsync(c);
                    }
                }

                //get badges
                var qll = await DabService.GetUpdatedBadges(GlobalResources.BadgesUpdatedDate);
                if (qll.Success)
                {
                    GlobalResources.BadgesUpdatedDate = DateTime.Now;
                    foreach (var d in qll.Data)
                    {
                        foreach (var b in d.payload.data.updatedBadges.edges)
                        {
                            await adb.InsertOrReplaceAsync(b);
                        }
                    }
                }

                //get campaigns
                var qlll = await DabService.GetCampaigns(GlobalResources.CampaignUpdatedDate);
                if (qlll.Success)
                {
                    foreach (var d in qlll.Data)
                    {
                        foreach (var b in d.payload.data.updatedCampaigns.edges)
                        {
                            dbCampaigns c = new dbCampaigns(b);
                            if (c.pricingPlans != null)
                            {
                                foreach (var plan in b.pricingPlans)
                                {
                                    dbPricingPlans newPlan = new dbPricingPlans(plan);
                                    dbCampaignHasPricingPlan hasPricingPlan = adb.Table<dbCampaignHasPricingPlan>().Where(x => x.CampaignId == c.campaignId && x.CampaignWpId == c.campaignWpId && x.PricingPlanId == newPlan.id).FirstOrDefaultAsync().Result;
                                    if (hasPricingPlan == null)
                                    {
                                        hasPricingPlan = new dbCampaignHasPricingPlan();
                                        List<int> userPricingPlans = adb.Table<dbCampaignHasPricingPlan>().ToListAsync().Result.Select(x => x.Id).ToList();
                                        if (userPricingPlans.Count() == 0)
                                        {
                                            hasPricingPlan.Id = 0;
                                        }
                                        else
                                        {
                                            int newId = userPricingPlans.Max() + 1;
                                            hasPricingPlan.Id = newId;
                                        }

                                        hasPricingPlan.CampaignId = b.id;
                                        hasPricingPlan.CampaignWpId = b.wpId;
                                        hasPricingPlan.PricingPlanId = plan.id;
                                    }
                                    

                                    await adb.InsertOrReplaceAsync(hasPricingPlan);
                                    await adb.InsertOrReplaceAsync(newPlan);

                                }
                                await adb.InsertOrReplaceAsync(c);
                            }
                        }
                    }
                    //update date since last updated campaigns
                    GlobalResources.CampaignUpdatedDate = DateTime.Now;
                }

                //logged in user routines
                if (!GuestStatus.Current.IsGuestLogin)
                {

                    //post recent actions
                    await PostActionLogs();

                    //get user profile information and update it.
                    ql = await Service.DabService.GetUserData();
                    if (ql.Success == true) //ignore failures here
                    {
                        //process user profile information
                        var profile = ql.Data.payload.data.user;
                        await UpdateUserProfile(profile);
                    }

                    //get recent actions
                    await GetRecentActions();

                    //get badge progress tied to user
                    await GetUserBadgesProgress();

                    await GetUpdatedCreditCards();

                    await GetUpdatedDonationStatus();

                    await GetUpdatedDonationHistory();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task<bool> NotifyOnConnectionKeepAlive()
        {
            //Keepalive message received - nothing to do.
            return true;
        }

        #endregion

        #region Authentication Routines

        public static async Task<bool> CheckAndUpdateToken()
        {
            /* this method checks to see if the user's token needs to be renewed
            */
            try
            {
                var adb = DabData.AsyncDatabase;

                if (GuestStatus.Current.IsGuestLogin == false)
                {

                    if (DabService.IsConnected)
                    {

                        bool needsUpdate = false; //track if we need to update the token
                        try
                        {
                            //get the expiration date
                            DateTime creation = GlobalResources.Instance.LoggedInUser.TokenCreation;
                            int days = ContentConfig.Instance.options.token_life;
                            if (DateTime.Now > creation.AddDays(days))
                            {
                                needsUpdate = true; //token needs to be udpated
                            }
                            else
                            {
                                needsUpdate = false; // no update needed, it's still good
                            }
                        }
                        catch (Exception ex)
                        {
                            needsUpdate = true; //something went wrong - try to update
                        }

                        if (needsUpdate)
                        {
                            //update the token
                            var ql = await DabService.UpdateToken();
                            if (ql.Success)
                            {
                                //token was updated successfully
                                var newUserData = GlobalResources.Instance.LoggedInUser;
                                newUserData.Token = ql.Data.payload.data.updateToken.token;
                                newUserData.TokenCreation = DateTime.Now;
                                await adb.InsertOrReplaceAsync(newUserData);

                                //reset the connection using the new token
                                await DabService.TerminateConnection();
                                ql = await DabService.InitializeConnection();
                            }
                        }
                    }
                    return true; //token updated or didn't need updated
                }
                else
                {
                    return true; //nothing needed for guest
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        #endregion

        #region Channel and Episode Routines

        public static async Task<bool> GetEpisodes(int ChannelId, bool ReloadAll = false, bool FromResume = false)
        {
            /*
             * This method gets episodes for a channel based on the last query date, or back to the beginning, if requested
             */

            try
            {


                //Determine the start date
                DateTime startdate;
                int cnt = 0; //number of episodes updated;
                DateTime lastDate = DateTime.MinValue; //most recent episode date
                object source = new object();

                DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Checking for new episodes...", true));


                if (ReloadAll)
                {
                    startdate = GlobalResources.DabMinDate.ToUniversalTime();
                }
                else if (FromResume)
                {
                    startdate = GlobalResources.GetLastEpisodeQueryDate(ChannelId).AddDays(-1);
                }
                else
                {
                    startdate = GlobalResources.GetLastEpisodeQueryDate(ChannelId);
                }

                var adb = DabData.AsyncDatabase;
                var alreadyDownloadedEpisodes = adb.Table<dbEpisodes>().Where(x => x.channel_title == "Daily Audio Bible").Where(x => x.is_downloaded == true).ToListAsync().Result;
                List<int> downloadedEpsIds = new List<int>();
                foreach (var item in alreadyDownloadedEpisodes)
                {
                    downloadedEpsIds.Add((int)item.id);
                }
                var ql = await DabService.GetEpisodes(startdate, ChannelId);
                if (ql.Success)
                {
                    //store episodes in the database
                    var channel = await adb.Table<dbChannels>().Where(x => x.channelId == ChannelId).FirstOrDefaultAsync();

                    //loop through the episodes
                    foreach (var data in ql.Data)
                    {
                        DabGraphQlEpisodes episodes;
                        if (data!.payload!.data!.updatedEpisodes != null) //determine if we should use episodes or updatedepisodes object
                            episodes = data.payload.data.updatedEpisodes;  //use updatedepisodes if we got them (all but the first request)
                        else
                            episodes = data.payload.data.episodes; //use episdoes for the first time through.

                        foreach (var episode in episodes.edges)
                        {
                            //process each episode
                            cnt++;
                            if (episode.updatedAt > lastDate)
                            {
                                lastDate = episode.updatedAt;
                            }
                            dbEpisodes dbe = new dbEpisodes(episode);

                            //set up additional properties
                            var code = channel.key;
                            dbe.channel_code = code;
                            dbe.channel_title = channel.title;
                            if (downloadedEpsIds.Contains((int)dbe.id))
                            {
                                dbe.is_downloaded = true;
                                dbe.progressVisible = true;
                            }
                            else
                            {
                                dbe.is_downloaded = false;
                            }
                            if (GlobalResources.TestMode)
                            {
                                dbe.description += $" ({DateTime.Now.ToShortTimeString()})";
                            }

                            //add to database
                            await adb.InsertOrReplaceAsync(dbe);
                        }
                    }


                    if (cnt > 0)
                    {
                        //mark the last query date
                        GlobalResources.SetLastEpisodeQueryDate(ChannelId, lastDate.AddSeconds(1));//add a second to keep it from looping

                        //notify the UI
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            DabServiceEvents.EpisodesChanged();
                        });
                    }

                }
                else
                {
                    //nothing to do, no new episodes
                }

                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                object source = new object();
                DabUserInteractionEvents.WaitStopped(source, new EventArgs());
                return false;
            }
        }

        public static async Task<bool> EpisodePublished(DabGraphQlEpisode episode)
        {
            /* this episode adds an episode to the system as it's published
             */

            try
            {

                var adb = DabData.AsyncDatabase;
                dbEpisodes dbe = new dbEpisodes(episode);

                //find the channel
                int chanId = episode.channelId;
                var channel = await adb.Table<Channel>().Where(x => x.channelId == chanId).FirstAsync();

                //set up additional properties
                var code = channel.key;
                dbe.channel_code = code;
                dbe.channel_title = channel.title;
                dbe.is_downloaded = false;
                if (GlobalResources.TestMode)
                {
                    dbe.description += $" ({DateTime.Now.ToShortTimeString()})";
                }

                //add to database
                await adb.InsertOrReplaceAsync(dbe);

                //notify the UI
                DabServiceEvents.EpisodesChanged();

                return true;

            }
            catch (Exception ex)
            {
                //something went wrong.
                return false;
            }

        }



        //ACTION ROUTINES

        public static async Task<bool> GetRecentActions()
        {
            /*
             * This method gets recent actions from QL and posts them to the database.
             * It then raises messages to help the UI deal with them
             */

            try
            {
                if (GuestStatus.Current.IsGuestLogin == false)
                {

                    if (DabService.IsConnected)
                    {

                        //wait
                        object source = new object();
                        DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Getting your recent activity...", true));


                        //get last actions
                        var qlList = await DabService.GetActions(GlobalResources.LastActionDate);
                        if (qlList.Success == true) //failures are ok, but don't update the last action date.
                        {
                            //process all of the ql lists
                            foreach (var ql in qlList.Data)
                            {
                                var actions = ql.payload.data.lastActions.edges;
                                //process each list of actions within each item in the list
                                foreach (var action in actions)
                                {
                                    //update episode properties
                                    await PlayerFeedAPI.UpdateEpisodeUserData(action.episodeId, action.listen, action.favorite, action.hasJournal, action.position);
                                }
                            }
                            //store a new last action date
                            GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();

                            //raise an event to notify anything listening that new actions have been received
                            DabServiceEvents.EpisodeUserDataChanged();
                        }

                        //stop the wait indicator
                        DabUserInteractionEvents.WaitStopped(source, new EventArgs());
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }


        }

        public static async Task<bool> PostActionLogs()
        {
            /* 
             * this routine posts pending action logs
             * 
             * It only needs to be called in 2 places:
             * when the connection is established (to post any pending logs)
             * when a new log is created (ot post live logs while connected)
             * 
             * this routine returns true if all actions a processed and removed as expected.
             * it returns false if something goes wrong and actions still need to be processed.
             * 
             */

            try
            {
                if (GuestStatus.Current.IsGuestLogin == false)
                {
                    if (DabService.IsConnected == true)
                    {
                        //get actions to process
                        var adb = DabData.AsyncDatabase;
                        var actions = await adb.Table<dbPlayerActions>().ToListAsync();

                        //loop through actions
                        foreach (var action in actions)
                        {
                            DabServiceWaitResponse response;
                            var actionDate = action.ActionDateTime.Value.DateTime;
                            switch (action.ActionType.ToLower())
                            {
                                case "favorite":
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.Favorite, actionDate, action.Favorite, null);
                                    break;
                                case "listened_status":
                                //same as listened
                                case "listened":
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.Listened, actionDate, action.Listened, null);
                                    break;
                                case "pause":
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.PositionChanged, actionDate, null, Convert.ToInt32(action.PlayerTime));
                                    break;
                                case "entrydate":
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.Journaled, actionDate, true, null);
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            if (response.Success == true)
                            {
                                //check to ensure action was not overwritten by another newer action
                                var lastAction = response.Data.payload.data.logAction;
                                DateTime lastActionDateTime;
                                switch (action.ActionType.ToLower())
                                {
                                    case "favorite":
                                        lastActionDateTime = lastAction.favoriteUpdatedAt;
                                        break;
                                    case "listened_status":
                                    case "listened":
                                        lastActionDateTime = lastAction.listenUpdatedAt;
                                        break;
                                    case "pause":
                                        lastActionDateTime = lastAction.positionUpdatedAt;
                                        break;
                                    case "entrydate":
                                        lastActionDateTime = lastAction.entryDateUpdatedAt;
                                        break;
                                    default:
                                        lastActionDateTime = lastAction.updatedAt;
                                        break;
                                }
                                var lastActionAge = lastActionDateTime.Subtract(actionDate);
                                bool? hasJournal;
                                if (lastAction.entryDate != null)
                                    hasJournal = true;
                                else
                                    hasJournal = null;
                                if (lastActionAge.TotalMilliseconds > 1)  //Should be accurate to 0.000 
                                {
                                    await PlayerFeedAPI.UpdateEpisodeUserData(lastAction.episodeId, lastAction.listen, lastAction.favorite, hasJournal, lastAction.position, true);
                                    Debug.WriteLine($"Sent Action Date: {actionDate.TimeOfDay} \n" +
                                        $"Specific Updated At: {lastActionDateTime.TimeOfDay} \n" +
                                        $"Generic Updated At: {lastAction.updatedAt.TimeOfDay}");

                                }

                                //delete the action from the queue
                                await adb.DeleteAsync(action);
                            }
                            else
                            {
                                //leave the action alone, it will be processed later.
                                switch (response.ErrorMessage)
                                {
                                    case "Invalid episode id.": //probably a junk / 0 episode to be deleted
                                        Debug.WriteLine($"Deleting action for invaild episodeid {action.EpisodeId}.");
                                        await adb.DeleteAsync(action);
                                        break;

                                    default: // keep other episodes in the queue, will try again
                                        Debug.WriteLine($"Action failed to be processed: {JsonConvert.SerializeObject(action)} / ERROR: {response.ErrorMessage}");
                                        break;
                                }

                            }

                        }

                        //success!
                        return true;

                    }
                    else
                    {
                        //not connected - nothing to do
                        return false;
                    }
                }
                else
                {
                    //guest - nothing to do
                    return false;
                }
            }
            catch (Exception ex)
            {
                //something went wrong - needs to run again at some point.
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        public static async Task<bool> ReceiveActionLog(DabGraphQlAction action)
        {
            /*
             * This routine handles incoming action logs. 
             * It updates the database and notifies any listners of changes
             */

            await PlayerFeedAPI.UpdateEpisodeUserData(action.episodeId, action.listen, action.favorite, action.hasJournal, action.position, true);

            return true;
        }

        #endregion

        #region User Profile Routines
        public static async Task<GraphQlUser> UpdateUserProfile(GraphQlUser user)
        {
            /*this routine takes a graphql user object and updates
             * the local database with the new information.
             * It then sends out an event notifying anything listening 
             * the user profile has changed
             */


            //save user profile settings
            var adb = DabData.AsyncDatabase;

            dbUserData userData = GlobalResources.Instance.LoggedInUser;
            userData.WpId = user.wpId;
            userData.FirstName = user.firstName;
            userData.LastName = user.lastName;
            userData.NickName = user.nickname;
            userData.Email = user.email;
            userData.Language = user.language;
            userData.Channel = user.channel;
            userData.Channels = user.channels;
            userData.UserRegistered = user.userRegistered;

            await adb.InsertOrReplaceAsync(userData);
            //alert anything that is listening
            DabServiceEvents.UserProfileChanged(user);

            return user;

        }

        #endregion

        #region Wallet & Donation Routines

        internal static bool RecieveDonationSuccessMessage(DabGraphQlUpdateDonation data)
        {
            /*
             * This routine handles incoming donation update success/failure messages. 
             * It visually informs the user
             */

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Donation Updated", $"{data.message}", "OK");
            });

            return true;
        }

        internal static bool RecieveDeleteDonationSuccessMessage(DabGraphQlDeleteDonation data)
        {
            /*
             * This routine handles incoming donation update success/failure messages. 
             * It visually informs the user
             */

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Donation Updated", $"{data.message}", "OK");
            });

            return true;
        }

        internal static async Task<bool> ReceiveDonationUpdate(DabGraphQlDonation data)
        {
            /*
             * This routine handles incoming donation updates. 
             * It updates the database
             */

            try
            {
                var adb = DabData.AsyncDatabase;
                string id = data.id;
                dbUserCampaigns donation = adb.Table<dbUserCampaigns>().Where(x => x.Id == id).FirstOrDefaultAsync().Result;
                if (donation != null)
                {
                    donation.WpId = data.wpId;
                    donation.Amount = data.amount;
                    donation.RecurringInterval = data.recurringInterval;
                    donation.CampaignWpId = data.campaignWpId;
                    donation.Status = data.status;
                    //save cardid so we can tie it to source if we ever need it
                    donation.Source = data.source.cardId;
                    await adb.InsertOrReplaceAsync(donation);
                }
                else
                {
                    donation = new dbUserCampaigns(data);
                    await adb.InsertOrReplaceAsync(donation);
                }
                dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.donationId == data.id).FirstOrDefaultAsync().Result;
                if (source != null)
                {
                    source.cardId = data.source.cardId;
                    source.next = data.source.next;
                    source.processor = data.source.processor;
                    await adb.InsertOrReplaceAsync(source);
                }
                else
                {
                    dbCreditSource newSource = new dbCreditSource(data.source, donation.Id);
                    await adb.InsertOrReplaceAsync(newSource);
                }

                await DabServiceRoutines.GetUpdatedDonationHistory();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task<bool> RecieveCampaignUpdate(DabGraphQlUpdateCampaign data)
        {
            /*
             * This routine handles incoming campaign updates. 
             * It updates the database
             */

            try
            {
                var adb = DabData.AsyncDatabase;
                int id = data.id;
                dbCampaigns camp = adb.Table<dbCampaigns>().Where(x => x.campaignId == id).FirstOrDefaultAsync().Result;
                if (camp != null)
                {
                    camp.campaignDescription = data.description;
                    camp.campaignSuggestedRecurringDonation = data.suggestedRecurringDonation;
                    camp.campaignSuggestedSingleDonation = data.suggestedSingleDonation;
                    camp.campaignTitle = data.title;
                    camp.campaignStatus = data.status;

                    if (data.pricingPlans != null)
                    {
                        foreach (var plan in data.pricingPlans)
                        {
                            dbPricingPlans newPlan = new dbPricingPlans(plan);
                            int campId = data.id;
                            int campWpId = data.wpId;
                            string pricingPlanId = newPlan.id;
                            dbCampaignHasPricingPlan hasPricingPlan = adb.Table<dbCampaignHasPricingPlan>().Where(x => x.CampaignId == campId && x.CampaignWpId == campWpId && x.PricingPlanId == pricingPlanId).FirstOrDefaultAsync().Result;
                            if (hasPricingPlan == null)
                            {
                                hasPricingPlan = new dbCampaignHasPricingPlan();
                                List<int> userPricingPlans = adb.Table<dbCampaignHasPricingPlan>().ToListAsync().Result.Select(x => x.Id).ToList();
                                if (userPricingPlans.Count() == 0)
                                {
                                    hasPricingPlan.Id = 0;
                                }
                                else
                                {
                                    int newId = userPricingPlans.Max() + 1;
                                    hasPricingPlan.Id = newId;
                                }
                            }
                            hasPricingPlan.CampaignId = data.id;
                            hasPricingPlan.CampaignWpId = data.wpId;
                            hasPricingPlan.PricingPlanId = plan.id;

                            await adb.InsertOrReplaceAsync(hasPricingPlan);
                            await adb.InsertOrReplaceAsync(newPlan);

                        }
                        await adb.InsertOrReplaceAsync(camp);
                    }
                }
                else
                {
                    dbCampaigns newCamp = new dbCampaigns(data);
                    if (data.pricingPlans != null)
                    {
                        foreach (var plan in data.pricingPlans)
                        {
                            dbPricingPlans newPlan = new dbPricingPlans(plan);
                            int campId = data.id;
                            int campWpId = data.wpId;
                            string pricingPlanId = newPlan.id;
                            dbCampaignHasPricingPlan hasPricingPlan = adb.Table<dbCampaignHasPricingPlan>().Where(x => x.CampaignId == campId && x.CampaignWpId == campWpId && x.PricingPlanId == pricingPlanId).FirstOrDefaultAsync().Result;
                            if (hasPricingPlan == null)
                            {
                                hasPricingPlan = new dbCampaignHasPricingPlan();
                                List<int> userPricingPlans = adb.Table<dbCampaignHasPricingPlan>().ToListAsync().Result.Select(x => x.Id).ToList();
                                if (userPricingPlans.Count() == 0)
                                {
                                    hasPricingPlan.Id = 0;
                                }
                                else
                                {
                                    int newId = userPricingPlans.Max() + 1;
                                    hasPricingPlan.Id = newId;
                                }
                            }
                            hasPricingPlan.CampaignId = data.id;
                            hasPricingPlan.CampaignWpId = data.wpId;
                            hasPricingPlan.PricingPlanId = plan.id;

                            await adb.InsertOrReplaceAsync(hasPricingPlan);
                            await adb.InsertOrReplaceAsync(newPlan);

                        }
                        await adb.InsertOrReplaceAsync(camp);
                    }
                    await adb.InsertOrReplaceAsync(newCamp);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task GetUpdatedDonationHistory()
        {
            var adb = DabData.AsyncDatabase;
            DateTime LastDate = GlobalResources.UserDonationHistoryUpdateDate;

            var qlll = await DabService.GetUserDonationHistoryUpdate(LastDate);
            if (qlll.Success == true)
            {
                try
                {
                    foreach (var item in qlll.Data)
                    {
                        //find donations by donationId and update status if changed
                        foreach (var d in item.payload.data.updatedDonationHistory.edges)
                        {
                            string id = d.id;
                            dbDonationHistory data = adb.Table<dbDonationHistory>().Where(x => x.historyId == id).FirstOrDefaultAsync().Result;
                            if (data == null)
                            {
                                dbDonationHistory newHist = new dbDonationHistory(d);
                                await adb.InsertOrReplaceAsync(newHist);
                            }
                            else if (data != null)
                            {
                                data.historyCampaignWpId = d.campaignWpId;
                                data.historyChargeId = d.chargeId;
                                data.historyCurrency = d.currency;
                                data.historyDate = d.date;
                                data.historyDonationType = d.donationType;
                                data.historyFee = d.fee;
                                data.historyGrossDonation = d.grossDonation;
                                data.historyNetDonation = d.netDonation;
                                data.historyPaymentType = d.paymentType;
                                data.historyPlatform = d.platform;
                                data.historyWpId = d.wpId;
                                //insert new donation history data
                                await adb.InsertOrReplaceAsync(data);
                            }
                        }
                    }
                    GlobalResources.UserDonationHistoryUpdateDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while updating user donation status: {ex.Message}");
                }
            }
        }

        public static async Task GetUpdatedDonationStatus()
        {
            var adb = DabData.AsyncDatabase;
            DateTime LastDate = GlobalResources.UserDonationStatusUpdateDate;

            var qlll = await DabService.GetUserDonationStatusUpdate(LastDate);
            if (qlll.Success == true)
            {
                try
                {
                    foreach (var item in qlll.Data)
                    {
                        //find donations by donationId and update status if changed
                        foreach (var d in item.payload.data.updatedDonationStatus.edges)
                        {
                            string id = d.id;
                            dbUserCampaigns data = adb.Table<dbUserCampaigns>().Where(x => x.Id == id).FirstOrDefaultAsync().Result;
                            if (data == null)
                            {
                                dbUserCampaigns newCamp = new dbUserCampaigns(d);
                                await adb.InsertOrReplaceAsync(newCamp);
                            }
                            else if (data != null)
                            {
                                data.Amount = d.amount;
                                data.CampaignWpId = d.campaignWpId;
                                data.RecurringInterval = d.recurringInterval;
                                //save cardid so we can tie it to source when we need it
                                data.Source = d.source.cardId;
                                data.Status = d.status;
                                //insert new card data
                                await adb.InsertOrReplaceAsync(data);
                            }
                            //save card source tied to donation
                            //string cardId = d.id;
                            dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.donationId == d.id).FirstOrDefaultAsync().Result;
                            if (source != null)
                            {
                                source.donationId = d.id;
                                source.next = d.source.next;
                                source.processor = d.source.processor;
                                await adb.InsertOrReplaceAsync(source);
                            }
                            else
                            {
                                dbCreditSource newSource = new dbCreditSource(d.source, d.id);
                                await adb.InsertOrReplaceAsync(newSource);
                            }
                        }
                    }
                    GlobalResources.UserDonationStatusUpdateDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while updating user donation status: {ex.Message}");
                }
            }
        }

        public static async Task GetUpdatedCreditCards()
        {
            var adb = DabData.AsyncDatabase;
            DateTime LastDate = GlobalResources.UserCreditCardUpdateDate;

            var qlll = await DabService.GetUsersUpdatedCreditCards(LastDate);
            if (qlll.Success == true)
            {
                try
                {
                    foreach (var item in qlll.Data)
                    {
                        //reverse order incase multiple changes to same card and most current instance will come through last 
                        item.payload.data.updatedCards.Reverse();
                        foreach (var d in item.payload.data.updatedCards)
                        {
                            int wpId = d.wpId;
                            dbCreditCards data = adb.Table<dbCreditCards>().Where(x => x.cardWpId == wpId).FirstOrDefaultAsync().Result;
                            if (data == null)
                            {
                                dbCreditCards newCard = new dbCreditCards();

                                newCard.cardExpMonth = d.expMonth;
                                newCard.cardExpYear = d.expYear;
                                newCard.cardLastFour = d.lastFour;
                                newCard.cardStatus = d.status;
                                newCard.cardType = d.type;
                                newCard.cardUserId = d.userId;
                                newCard.cardWpId = d.wpId;
                                //insert new card data
                                await adb.InsertOrReplaceAsync(newCard);
                            }
                            else if (data != null)
                            {
                                data.cardStatus = d.status;
                                //update card status
                                await adb.InsertOrReplaceAsync(data);
                            }
                        }

                    }

                    //update last time checked for badge progress
                    GlobalResources.UserCreditCardUpdateDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while updating user credit cards: {ex.Message}");
                }
            }
        }

        public static async Task UpdateCreditCard(DabGraphQlCreditCard data)
        {
            var adb = DabData.AsyncDatabase;
            int wpId = data.wpId;
            dbCreditCards card = adb.Table<dbCreditCards>().Where(x => x.cardWpId == wpId).FirstOrDefaultAsync().Result;
            if (card == null)
            {
                dbCreditCards newCard = new dbCreditCards();

                newCard.cardExpMonth = data.expMonth;
                newCard.cardExpYear = data.expYear;
                newCard.cardLastFour = data.lastFour;
                newCard.cardStatus = data.status;
                newCard.cardType = data.type;
                newCard.cardUserId = data.userId;
                newCard.cardWpId = data.wpId;
                //insert new card data
                await adb.InsertOrReplaceAsync(newCard);
            }
            else if (card != null)
            {
                card.cardStatus = data.status;
                //update card status
                await adb.InsertOrReplaceAsync(card);
            }
        }

        #endregion

        #region Badge and Progress Routines

        public static async Task GetUserBadgesProgress()
        {
            var adb = DabData.AsyncDatabase;
            userName = GlobalResources.Instance.LoggedInUser.Email;

            //get user badge progress
            DateTime LastDate = GlobalResources.BadgeProgressUpdatesDate;
            var qlll = await DabService.GetUserProgress(LastDate);
            if (qlll.Success == true)
            {
                try
                {
                    foreach (var item in qlll.Data)
                    {
                        foreach (var d in item.payload.data.updatedProgress.edges)
                        {
                            int id = d.id;
                            dbUserBadgeProgress data = adb.Table<dbUserBadgeProgress>().Where(x => x.id == id && x.userName == userName).FirstOrDefaultAsync().Result;

                            //set percentage to 1 to make visible even if 0 (received as an int)
                            if (d.percent <= 0)
                            {
                                d.percent = 1;
                            }

                            if (data == null)
                            {
                                d.userName = userName;
                                await adb.InsertOrReplaceAsync(d);
                            }
                            else
                            {
                                data.percent = d.percent;
                                await adb.InsertOrReplaceAsync(data);
                            }

                        }

                    }
                    //update last time checked for badge progress
                    GlobalResources.BadgeProgressUpdatesDate = DateTime.UtcNow;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while grabbing user's badge progress: {ex.Message}");
                }

            }
        }

        public static async Task<bool> SeeProgress(int ProgressId)
        {

            //wait for graphql
            var rv = await Service.DabService.SeeProgress(ProgressId);

            if (rv.Success)
            {
                //mark databas as seen locally
                var adb = DabData.AsyncDatabase;
                dbUserBadgeProgress badgeData = adb.Table<dbUserBadgeProgress>().Where(x => x.id == ProgressId && x.userName == userName).FirstOrDefaultAsync().Result;
                if (badgeData != null)
                {
                    badgeData.seen = true;
                }
            }
            return rv.Success;
        }

        public static async Task UpdateProgress(DabGraphQlProgressUpdated data)
        {
            var adb = DabData.AsyncDatabase;
            userName = GlobalResources.Instance.LoggedInUser.Email;
            //bool badgeFirstEarned = false;

            //Build out progress object
            DabGraphQlProgress progress = data.progress;
            if (progress.percent == 100 && (progress.seen == null || progress.seen == false))
            {
                //log to firebase
                var fbInfo = new Dictionary<string, string>();
                fbInfo.Add("user", userName);
                fbInfo.Add("idiom", Device.Idiom.ToString());
                fbInfo.Add("badgeId", progress.badgeId.ToString());
                DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_progressAchieved", fbInfo);

                await PopupNavigation.Instance.PushAsync(new AchievementsProgressPopup(progress));
            }

            //Save badge progress data
            int progressId = progress.id;
            dbUserBadgeProgress badgeData = adb.Table<dbUserBadgeProgress>().Where(x => x.id == progressId && x.userName == userName).FirstOrDefaultAsync().Result;
            try
            {
                //set percentage to 1 to make visible even if 0 (received as an int)
                if (progress.percent <= 0)
                {
                    progress.percent = 1;
                }

                if (badgeData == null)
                {
                    //new user badge progress
                    badgeData = new dbUserBadgeProgress(progress, userName);
                }
                else
                {
                    //existing user badge progress
                    badgeData.percent = progress.percent;
                }
                await adb.InsertOrReplaceAsync(badgeData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving badge / progress data: {JsonConvert.SerializeObject(progress)}: {ex.Message}");
            }
        }

        public static async Task RemoveToken()
        {
            //log to firebase
            var adb = DabData.AsyncDatabase;
            var fbInfo = new Dictionary<string, string>();
            string email = GlobalResources.Instance.LoggedInUser.Email;
            fbInfo.Add("user", email);
            fbInfo.Add("idiom", Device.Idiom.ToString());
            DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_forcefulLogoutViaSubscription", fbInfo);


            await GlobalResources.LogoffAndResetApp("You have been logged out of all your devices.");
        }
    }

        #endregion
}