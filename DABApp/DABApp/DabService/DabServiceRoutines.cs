using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.DabSockets;
using Newtonsoft.Json;
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
                //TODO: get recent episodes
                //TODO: get recent badges

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

                    //get recent progress
                    //TODO: get recent progress
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
            //Keepalive message received - let the UI do something about it
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

                GlobalResources.WaitStart("Checking for new episodes...");

                if (ReloadAll)
                {
                    startdate = GlobalResources.DabMinDate;
                }
                else
                {
                    startdate = GlobalResources.GetLastEpisodeQueryDate(ChannelId);
                }

                var ql = await DabService.GetEpisodes(startdate, ChannelId);
                if (ql.Success)
                {
                    //store episodes in the database
                    var adb = DabData.AsyncDatabase;
                    var channel = await adb.Table<dbChannels>().Where(x => x.channelId == ChannelId).FirstOrDefaultAsync();

                    //loop through the episodes
                    foreach (var data in ql.Data)
                    {
                        var episodes = data.payload.data.episodes;
                        foreach (var episode in episodes.edges)
                        {
                            //process each episode
                            cnt++;
                            dbEpisodes dbe = new dbEpisodes(episode);

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
                        }
                    }


                    if (cnt > 0)
                    {
                        //mark the last query date
                        GlobalResources.SetLastEpisodeQueryDate(ChannelId);

                        //notify the UI
                        //TODO: Confirm all of these messages
                        Device.BeginInvokeOnMainThread(() =>
                    {
                        MessagingCenter.Send<string>("Update", "Update");
                        MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                        MessagingCenter.Send<string>("dabapp", "OnEpisodesUpdated");
                        MessagingCenter.Send<string>("dabapp", "ShowTodaysEpisode");

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
                //TODO: Confirm all of these messages
                Device.BeginInvokeOnMainThread(() =>
                {
                    MessagingCenter.Send<string>("Update", "Update");
                    MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                    MessagingCenter.Send<string>("dabapp", "OnEpisodesUpdated");
                    MessagingCenter.Send<string>("dabapp", "ShowTodaysEpisode");

                });

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
                            DabServiceEvents.EpisodesChanged();
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
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.Favorite, actionDate, action.Favorite,null);
                                    break;
                                case "listened_status":
                                    //same as listened
                                case "listened":
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.Listened, actionDate, action.Listened,null);
                                    break;
                                case "pause":
                                    response = await DabService.LogAction(action.EpisodeId, ServiceActionsEnum.PositionChanged, actionDate, null, Convert.ToInt32(action.PlayerTime) );
                                    break;
                                case "entryDate":
                                    //TODO: Implement this
                                    throw new NotSupportedException("Journals not working yet.");
                                default:
                                    throw new NotSupportedException();
                            }

                            if (response.Success == true)
                            {
                                //check to ensure action was not overwritten by another newer action
                                var lastAction = response.Data.payload.data.logAction;
                                var lastActionAge = lastAction.updatedAt.Subtract(actionDate);
                                if (lastActionAge.TotalSeconds>1) //TODO: This should be replaced with action update dates that match per LUTD 7/7/2020
                                {
                                    await PlayerFeedAPI.UpdateEpisodeUserData(lastAction.episodeId, lastAction.listen, lastAction.favorite, null, lastAction.position, true);
                                    Debug.WriteLine($"Action was overwritten by newer action. OLD: {JsonConvert.SerializeObject(action)} / NEW: {JsonConvert.SerializeObject(lastAction)}");
                                }

                                //delete the action from the queue
                                await adb.DeleteAsync(action);
                            } else {
                                //leave the action alone, it will be processed later.
                                //other actions may continue to get processed.
                                Debug.WriteLine($"Action failed to be processed: {JsonConvert.SerializeObject(action)} / ERROR: {response.ErrorMessage}");
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
                } else
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

        #endregion

        #region User Profile Routines
        public static async Task<GraphQlUser> UpdateUserProfile (GraphQlUser user)
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

            //alert anything that is listening
            DabServiceEvents.UserProfileChanged(user);

            return user;

        }
        #endregion

    }
}


