using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.DabUI;
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
                //TODO: fill these in

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

                if (GlobalResources.Instance.IsLoggedIn)
                {

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

                if (GuestStatus.Current.IsGuestLogin == false)
                {

                    if (DabService.IsConnected)
                    {

                        bool needsUpdate = false; //track if we need to update the token
                        try
                        {
                            //get the expiration date
                            var creation = DateTime.Parse(dbSettings.GetSetting("TokenCreation", DateTime.MinValue.ToString()));
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
                                dbSettings.StoreSetting("Token", ql.Data.payload.data.updateToken.token);
                                dbSettings.StoreSetting("TokenCreation", DateTime.Now.ToString());

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

        public static async Task<bool> GetEpisodes(int ChannelId, bool ReloadAll = false)
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

                GlobalResources.WaitStart("Checking for new episodes...");

                if (ReloadAll)
                {
                    startdate = GlobalResources.DabMinDate.ToUniversalTime();
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
                        //TODO: Confirm all of these messages
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

                GlobalResources.WaitStop();
                return true;
            }
            catch (Exception ex)
            {
                GlobalResources.WaitStop();
                return false;
            }
        }

        public static async Task<bool> EpisodePublished(DabGraphQlEpisode episode)
        {
            /* this episode adds an episode to the system as it's published
             */

            //TODO: This has not been tested.

            try
            {

                var adb = DabData.AsyncDatabase;
                dbEpisodes dbe = new dbEpisodes(episode);

                //find the channel
                var channel = await adb.Table<Channel>().Where(x => x.channelId == episode.channelId).FirstAsync();

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
                        GlobalResources.WaitStart("Getting your recent activity...");

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
                        GlobalResources.WaitStop();
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
                                    //TODO: Implement this
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.Journaled, actionDate, true, null);
                                    //throw new NotSupportedException("Journals not working yet.");
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
            dbSettings.StoreSetting("FirstName", user.firstName);
            dbSettings.StoreSetting("LastName", user.lastName);
            dbSettings.StoreSetting("Email", user.email);
            dbSettings.StoreSetting("Nickname", user.nickname);
            dbSettings.StoreSetting("Channel", user.channel);
            dbSettings.StoreSetting("Channels", user.channels);
            dbSettings.StoreSetting("Language", user.language);
            dbSettings.StoreSetting("WpId", user.wpId.ToString());

            //alert anything that is listening
            DabServiceEvents.UserProfileChanged(user);

            return user;

        }

        #endregion

        #region Badge and Progress Routines

        public static async Task GetUserBadgesProgress()
        {
            var adb = DabData.AsyncDatabase;
            userName = dbSettings.GetSetting("Email", "");

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
                            dbUserBadgeProgress data = adb.Table<dbUserBadgeProgress>().Where(x => x.id == d.id && x.userName == userName).FirstOrDefaultAsync().Result;

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
                    string settingsKey = $"BadgeProgressDate-{dbSettings.GetSetting("Email", "")}";
                    dbSettings.StoreSetting(settingsKey, DateTime.UtcNow.ToString());
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
            userName = dbSettings.GetSetting("Email", "");

            //Build out progress object
            DabGraphQlProgress progress = data.progress ;
            if (progress.percent == 100 && (progress.seen == null || progress.seen == false))
            {
                //log to firebase
                var fbInfo = new Dictionary<string, string>();
                fbInfo.Add("user", dbSettings.GetSetting("Email", ""));
                fbInfo.Add("idiom", Device.Idiom.ToString());
                fbInfo.Add("badgeId", progress.badgeId.ToString());
                DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_progressAchieved", fbInfo);

                await PopupNavigation.Instance.PushAsync(new AchievementsProgressPopup(progress));
            }
            
            //Save badge progress data
            dbUserBadgeProgress badgeData = adb.Table<dbUserBadgeProgress>().Where(x => x.id == progress.id && x.userName == userName).FirstOrDefaultAsync().Result;
            try
            {
                if (badgeData == null)
                {
                    //new user badge progress
                    badgeData = new dbUserBadgeProgress(progress, userName);
                    await adb.InsertOrReplaceAsync(badgeData);
                }
                else
                {
                    //existing user badge progress
                    badgeData.percent = progress.percent;
                    await adb.InsertOrReplaceAsync(badgeData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving badge / progress data: {JsonConvert.SerializeObject(progress)}: {ex.Message}");
            }
        }

        public static async Task RemoveToken()
        {
            //log to firebase
            var fbInfo = new Dictionary<string, string>();
            fbInfo.Add("user", dbSettings.GetSetting("Email", ""));
            fbInfo.Add("idiom", Device.Idiom.ToString());
            DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_forcefulLogoutViaSubscription", fbInfo);


            await GlobalResources.LogoffAndResetApp("You have been logged out of all your devices.");
        }

        #endregion
    }
}


