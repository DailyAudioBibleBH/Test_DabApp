using System;
using System.Threading.Tasks;

namespace DABApp.Service
{
    public static class DabServiceRoutines
    {
        /* 
         * This class is focused on common routines used by DabService throughout the app that involve more than just querying the service. 
         * Methods in this class may interact with the database, take UI elements as arguments to update, or send messages.
         * It is important to leave DabService class focused on GraphQL interaction only.
         */

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

                //logged in user routines
                if (! GuestStatus.Current.IsGuestLogin)
                {

                    //get user profile information and update it.
                    var ql = await Service.DabService.GetUserData();
                    if (ql.Success == true) //ignore failures here
                    {
                        //process user profile information
                        var profile = ql.Data.payload.data.user;
                        dbSettings.StoreSetting("FirstName", profile.firstName);
                        dbSettings.StoreSetting("LastName", profile.lastName);
                        dbSettings.StoreSetting("Email", profile.email);
                        dbSettings.StoreSetting("Channel", profile.channel);
                        dbSettings.StoreSetting("Channels", profile.channels);
                        dbSettings.StoreSetting("Language", profile.language);
                        dbSettings.StoreSetting("Nickname", profile.nickname);
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

        public static async Task<bool> GetRecentActions()
        {
            /*
             * This method gets recent actions from QL and posts them to the database.
             * It then raises messages to help the UI deal with them
             */

            try
            {

                //wait
                GlobalResources.WaitStart("Getting your recent activity...");

                //get last actions
                var qlList = await DabService.GetActions(GlobalResources.LastActionDate);
                if (qlList.Success == true) //ignore failures here
                {
                    //process all of the ql lists
                    foreach (var ql in qlList.Data)
                    {
                        var actions = ql.payload.data.lastActions.edges;
                        //process each list of actions within each item in the list
                        foreach (var action in actions)
                        {
                            //update episode properties
                            await PlayerFeedAPI.UpdateEpisodeProperty(action.episodeId, action.listen, action.favorite, action.hasJournal, action.position);
                        }
                    }
                    //store a new last action date
                    GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();
                }

                //stop the wait indicator
                GlobalResources.WaitStop();

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }


        }


    }
}
