using DABApp.DabUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rg.Plugins.Popup.Services;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Security.Cryptography;
using System.Text;

namespace DABApp.DabSockets
{
    public class DabSyncService : INotifyPropertyChanged
    {

        /* This is the sync service that manages connections with the DAB back end
         *
         * To use it, refer to DabSyncService.Instance
         * 
         * It currently handles:
         * * sending events to server for favorite, listened, progresss
         */


        public static DabSyncService Instance = new DabSyncService();

        IWebSocket sock; //The socket connection

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DabGraphQlMessageEventHandler> DabGraphQlMessage; //Event so others can listen in on events.

        SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors
        DabGraphQlVariables variables = new DabGraphQlVariables();

        string origin;

        int channelId;
        dbChannels channel = new dbChannels();
        List<DabGraphQlEpisode> allEpisodes = new List<DabGraphQlEpisode>();
        DabEpisodesPage episodesPage;

        List<int> subscriptionIds = new List<int>();
        string userName;
        public int popRequests = 0;

        bool GraphQlLoginRequestInProgress = false;
        bool GraphQlLoginComplete = false;

        private DabSyncService()
        {
            //Constructure is private so we only allow one of them
        }

        

        private async void Sock_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            userName = GlobalResources.GetUserEmail();
            DabGraphQlMessage?.Invoke(this, e);

            try
            {
                var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);


                //Generic keep alive
                if (root.type == "ka")
                {
                    //Nothing to see here...
                    return;
                }

                //Check for error messages
                if (root.type == "error" && root.payload?.message != null)
                {
                    if (root.payload.message == "Your token is not valid.")
                    {
                        //Clean up settings we don't want anymore.
                        dbSettings s = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                        if (s != null)
                        {
                            //log to firebase
                            var fbInfo = new Dictionary<string, string>();
                            fbInfo.Add("user", GlobalResources.GetUserEmail());
                            fbInfo.Add("idiom", Device.Idiom.ToString());
                            DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_forcefulLogoutViaToken", fbInfo);

                            //User's token is no longer good. Better log them off (which will delete the settings)
                            GlobalResources.LogoffAndResetApp("Your login token is not valid. Please log back in.");

                        }
                        return;
                    }
                    else //other errors 
                    {
                        //log error to firebase
                        var errorInfo = new Dictionary<string, string>();
                        errorInfo.Add("user", GlobalResources.GetUserEmail());
                        errorInfo.Add("idiom", Device.Idiom.ToString());
                        errorInfo.Add("error", $"Payload.Message: {root.payload.message}");
                        DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_error", errorInfo);
                    }
                }

                //logging errors, but not doing anything else with them right now.
                if (root.payload?.errors != null)
                {
                    //turn off any lingering wait indicators to allow them to continue trying.
                    GlobalResources.WaitStop(); //Stop the wait indicator... something went wrong, hopefully they can work around it or try again.

                    if (root?.payload?.errors?.First().message == "Not authorized.")
                    {
                        //Token
                        dbSettings s = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                        if (s != null) await adb.DeleteAsync(s);

                        //TokenCreation
                        s = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                        if (s != null) await adb.DeleteAsync(s);

                        DabGraphQlVariables variables = new DabGraphQlVariables();
                        var exchangeTokenQuery = "mutation { updateToken(version: 1) { token } }";
                        var exchangeTokenPayload = new DabGraphQlPayload(exchangeTokenQuery, variables);
                        var tokenJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", exchangeTokenPayload));
                        DabSyncService.Instance.Send(tokenJsonIn);
                        GlobalResources.WaitStop();
                        Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.DisplayAlert("Token Error", "We're updating your session token. Please try signing up again.", "OK"); ; });
                    }
                    else if (root?.payload?.errors?.First() != null)
                    {
                        if (GraphQlLoginRequestInProgress == true)
                        {
                            GlobalResources.WaitStop();
                            //We have a login error!
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Application.Current.MainPage.DisplayAlert("Login Error", root.payload.errors.First().message, "OK");

                            });
                            GraphQlLoginRequestInProgress = false;
                        }
                        else
                        {
                            GlobalResources.WaitStop();
                            //We have an error!
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Application.Current.MainPage.DisplayAlert("Error", root.payload.errors.First().message, "OK");
                            });
                        }
                    }
                    else
                    {
                        //Some other GraphQL message we don't care about here.

                    }

                    foreach (var er in root.payload.errors)
                    {
                        //log error to firebase
                        var errorInfo = new Dictionary<string, string>();
                        errorInfo.Add("user", GlobalResources.GetUserEmail());
                        errorInfo.Add("idiom", Device.Idiom.ToString());
                        errorInfo.Add("error", $"Payload.Error: {er.message}");
                        DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_error", errorInfo);
                    }

                }

                //Confirmation of logAction being received - delete items from the queue that we need to.
                if (root.payload?.data?.logAction != null)
                {
                    //TODO: Only delete actions tied to the correct user id. right now we don't check that because we store user email, not user id.
                    var action = root.payload.data.logAction;
                    var actions = new List<dbPlayerActions>();
                    if (action.listen != null)
                    {
                        actions = await adb.Table<dbPlayerActions>().Where(x => x.EpisodeId == action.episodeId && action.listen == action.listen).ToListAsync();
                    }
                    else if (action.favorite != null)
                    {
                        actions = await adb.Table<dbPlayerActions>().Where(x => x.EpisodeId == action.episodeId && action.favorite == action.favorite).ToListAsync();
                    }
                    else if (action.position != null)
                    {
                        actions = await adb.Table<dbPlayerActions>().Where(x => x.EpisodeId == action.episodeId && action.position == action.position).ToListAsync();
                    }
                    else if (action.entryDate != null)
                    {
                        actions = await adb.Table<dbPlayerActions>().Where(x => x.EpisodeId == action.episodeId && action.entryDate == action.entryDate).ToListAsync();
                    }

                    foreach (var a in actions)
                    {
                        Debug.WriteLine($"Deleting action log from local database: {JsonConvert.SerializeObject(a)}");
                        await adb.DeleteAsync(a);
                    }

                    //update the database with the correct value, if it's different
                    bool hasJournal;
                    if (action.entryDate != null)
                        hasJournal = true;
                    else
                        hasJournal = false;

                    await PlayerFeedAPI.UpdateEpisodeProperty(action.episodeId, action.listen, action.favorite, hasJournal, action.position, false);
                }

                //Action we need to address
                if (root.payload?.data?.actionLogged != null)
                {
                    var action = root.payload.data.actionLogged.action;
                    bool hasJournal;

                    if (action.entryDate != null)
                        hasJournal = true;
                    else
                        hasJournal = false;

                    //Need to figure out action type
                    await PlayerFeedAPI.UpdateEpisodeProperty(action.episodeId, action.listen, action.favorite, hasJournal, action.position);
                }
                else if (root?.payload?.data?.loginUser != null)
                {

                    //Store the token
                    dbSettings sToken = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                    if (sToken == null)
                    {
                        sToken = new dbSettings() { Key = "Token" };
                    }
                    sToken.Value = root.payload.data.loginUser.token;
                    await adb.InsertOrReplaceAsync(sToken);

                    //Update Token Life
                    ContentConfig.Instance.options.token_life = 5;
                    dbSettings sTokenCreationDate = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                    if (sTokenCreationDate == null)
                    {
                        sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
                    }
                    sTokenCreationDate.Value = DateTime.Now.ToString();
                    await adb.InsertOrReplaceAsync(sTokenCreationDate);

                    DabSyncService.Instance.ConnectGraphQl(sToken.Value);

                    //Send a request for updated user data
                    string jUser = $"query {{user{{wpId,firstName,lastName,email}}}}";
                    var pLogin = new DabGraphQlPayload(jUser, new DabGraphQlVariables());
                    DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", pLogin)));

                }
                else if (root.payload?.data?.channels != null)
                {
                    foreach (var item in root.payload.data.channels)
                    {
                        await adb.InsertOrReplaceAsync(item);
                    }
                }
                //process incoming lastActions
                else if (root.payload?.data?.lastActions != null)
                {
                    if (GlobalResources.GetUserEmail() != "Guest")
                    {
                        GlobalResources.WaitStart("Please wait while we load your personal action history. Depending on your internet connection, this could take up to a minute.");


                        List<DabGraphQlEpisode> actionsList = new List<DabGraphQlEpisode>();  //list of actions
                        if (root.payload.data.lastActions.pageInfo.hasNextPage == true)
                        {
                            foreach (DabGraphQlEpisode item in root.payload.data.lastActions.edges.OrderByDescending(x => x.createdAt))  //loop throgh them all and update episode data (without sending episode changed messages)
                            {
                                await PlayerFeedAPI.UpdateEpisodeProperty(item.episodeId, item.listen, item.favorite, item.hasJournal, item.position, false);
                            }
                            //since we told UpdateEpisodeProperty to NOT send a message to the UI, we need to do that now.
                            if (root.payload.data.lastActions.edges.Count > 0)
                            {
                                //TODO I would like to take messaging center out of here but need to figure how to grab resource parameter
                                MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                            }

                            //Send last action query to the websocket
                            //TODO: Come back and clean up with GraphQl objects
                            System.Diagnostics.Debug.WriteLine($"Getting actions since {GlobalResources.LastActionDate.ToString()}...");
                            var updateEpisodesQuery = "{ lastActions(date: \"" + GlobalResources.LastActionDate.ToString("o") + "Z\", cursor: \"" + root.payload.data.lastActions.pageInfo.endCursor + "\") { edges { id episodeId userId favorite listen position entryDate updatedAt createdAt } pageInfo { hasNextPage endCursor } } } ";
                            var updateEpisodesPayload = new DabGraphQlPayload(updateEpisodesQuery, variables);
                            var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", updateEpisodesPayload));
                            DabSyncService.Instance.Send(JsonIn);
                        }
                        else
                        {
                            if (root.payload.data.lastActions != null)
                            {
                                foreach (DabGraphQlEpisode item in root.payload.data.lastActions.edges.OrderByDescending(x => x.createdAt))  //loop throgh them all and update episode data (without sending episode changed messages)
                                {
                                    await PlayerFeedAPI.UpdateEpisodeProperty(item.episodeId, item.listen, item.favorite, item.hasJournal, item.position, false);
                                }
                                //since we told UpdateEpisodeProperty to NOT send a message to the UI, we need to do that now.
                                if (root.payload.data.lastActions.edges.Count > 0)
                                {
                                    MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                                }
                            }

                            //store a new last action date
                            GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();
                            GlobalResources.WaitStop();

                        }
                    }

                }
                //Grabbing episodes
                else if (root.payload?.data?.episodes != null)
                {
                    GlobalResources.WaitStart("Please wait while we load the episode list...");
                    foreach (var item in root.payload.data.episodes.edges)
                    {
                        allEpisodes.Add(item);
                        channelId = item.channelId;
                    }

                    //Take action based on more pages or not
                    if (root.payload.data.episodes.pageInfo.hasNextPage == true)
                    {
                        //More pages, go get them
                        string lastEpisodeQueryDate = GlobalResources.GetLastEpisodeQueryDate(channelId);
                        Debug.WriteLine($"Getting episodes by ChannelId");
                        var episodesByChannelQuery = "query { episodes(date: \"" + lastEpisodeQueryDate + "\", channelId: " + channelId.ToString() + ", cursor: \"" + root.payload.data.episodes.pageInfo.endCursor + "\") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                        var episodesByChannelPayload = new DabGraphQlPayload(episodesByChannelQuery, variables);
                        var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", episodesByChannelPayload));
                        DabSyncService.Instance.Send(JsonIn);
                    }
                    else
                    {
                        //Last page, let UI know
                        var channels = adb.Table<dbChannels>().OrderByDescending(x => x.channelId).ToListAsync().Result;
                        foreach (var item in channels)
                        {
                            if (item.channelId == channelId)
                            {
                                channel = item;
                            }
                        }
                        if (root.payload.data.episodes != null)
                        {
                            await PlayerFeedAPI.GetEpisodes(allEpisodes, channel);
                            MessagingCenter.Send<string>("Update", "Update");
                            MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");

                            string favoriteChannel = dbSettings.GetSetting("Channel", "");
                            if (favoriteChannel != "")
                            {
                                dbChannels favChannel = adb.Table<dbChannels>().Where(x => x.title == favoriteChannel).FirstOrDefaultAsync().Result;
                                if (channel.id == favChannel.id)
                                {
                                    MessagingCenter.Send<string>("dabapp", "ShowTodaysEpisode");
                                }
                            }
                            await PlayerFeedAPI.DownloadEpisodes();

                        }
                        if (allEpisodes.Count() >= 1)
                        {
                            //store a new episode query date
                            GlobalResources.SetLastEpisodeQueryDate(channelId);
                        }
                        //stop wait ui on episodes page
                        GlobalResources.WaitStop();
                    }
                }
                else if (root.payload?.data?.episodePublished?.episode != null)
                {
                    //Get reference to the episode
                    var qlEpisode = root.payload.data.episodePublished.episode;

                    //Remove any existing references first
                    allEpisodes.RemoveAll(x => x.episodeId == qlEpisode.episodeId);
                    allEpisodes.Add(qlEpisode);
                    channelId = qlEpisode.channelId;

                    //find the matching channel
                    channel = adb.Table<dbChannels>().Where(x => x.channelId == channelId).FirstOrDefaultAsync().Result;
                    var code = channel.title == "Daily Audio Bible" ? "dab" : channel.title.ToLower();

                    //Add record to the database (or update it)
                    dbEpisodes newEpisode = new dbEpisodes(qlEpisode);
                    newEpisode.channel_code = code;
                    newEpisode.channel_title = channel.title;
                    newEpisode.is_downloaded = false;
                    if (GlobalResources.TestMode)
                    {
                        newEpisode.description += $" ({DateTime.Now.ToShortTimeString()})";
                    }
                    var x = adb.InsertOrReplaceAsync(newEpisode).Result;

                    //Notify listening items that episodes have changed.
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        MessagingCenter.Send<string>("Update", "Update");
                        MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                        MessagingCenter.Send<string>("dabapp", "OnEpisodesUpdated");
                        MessagingCenter.Send<string>("dabapp", "ShowTodaysEpisode");
                        //var x = PlayerFeedAPI.DownloadEpisodes().Result;

                    });
                }
                else if (root.payload?.data?.tokenRemoved?.token != null)
                {
                    //log to firebase
                    var fbInfo = new Dictionary<string, string>();
                    fbInfo.Add("user", GlobalResources.GetUserEmail());
                    fbInfo.Add("idiom", Device.Idiom.ToString());
                    DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_forcefulLogoutViaSubscription", fbInfo);


                    GlobalResources.LogoffAndResetApp("You have been logged out of all your devices.");
                }
                else if (root.payload?.data?.updateToken?.token != null)
                {
                    //Update Token
                    dbSettings sToken = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                    if (sToken == null)
                    {
                        sToken = new dbSettings() { Key = "Token" };
                    }
                    sToken.Value = root.payload.data.updateToken.token;
                    await adb.UpdateAsync(sToken);

                    //Update Token Life
                    dbSettings sTokenCreationDate = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                    if (sTokenCreationDate == null)
                    {
                        sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
                    }
                    sTokenCreationDate.Value = DateTime.Now.ToString();
                    await adb.InsertOrReplaceAsync(sTokenCreationDate);

                    Instance.DisconnectGraphQl(true);
                    Instance.ConnectGraphQl(root.payload.data.updateToken.token);
                }
                // check for changed in badges
                else if (root.payload?.data?.updatedBadges != null)
                {

                    if (root.payload?.data?.updatedBadges.edges.Count() > 0)
                    {
                        //add badges to db
                        foreach (var item in root.payload.data.updatedBadges.edges)
                        {
                            try
                            {
                                await adb.InsertOrReplaceAsync(item);

                            }
                            catch (Exception)
                            {
                                await adb.InsertOrReplaceAsync(item);
                            }
                        };
                    }

                    dbSettings sBadgeUpdateSettings = adb.Table<dbSettings>().Where(x => x.Key == "BadgeUpdateDate").FirstOrDefaultAsync().Result;
                    if (sBadgeUpdateSettings == null)
                    {
                        sBadgeUpdateSettings = new dbSettings() { Key = "BadgeUpdateDate" };
                    }
                    //Update date last time checked for badges
                    try
                    {
                        sBadgeUpdateSettings.Value = DateTime.UtcNow.ToString();
                        await adb.InsertOrReplaceAsync(sBadgeUpdateSettings);
                    }
                    catch (Exception)
                    {
                        sBadgeUpdateSettings.Value = DateTime.UtcNow.ToString();
                        await adb.InsertOrReplaceAsync(sBadgeUpdateSettings);
                    }
                }
                //progress towards achievements
                else if (root.payload?.data?.updatedProgress != null)
                {
                    foreach (var item in root.payload.data.updatedProgress.edges)
                    {
                        dbUserBadgeProgress data = adb.Table<dbUserBadgeProgress>().Where(x => x.id == item.id && x.userName == userName).FirstOrDefaultAsync().Result;
                        try
                        {
                            if (data == null)
                            {
                                item.userName = userName;
                                await adb.InsertOrReplaceAsync(item);
                            }
                            else
                            {
                                data.percent = item.percent;
                                await adb.InsertOrReplaceAsync(data);
                            }
                        }
                        catch (Exception)
                        {
                            if (data == null)
                            {
                                item.userName = userName;
                                await adb.InsertOrReplaceAsync(item);
                            }
                            else
                            {
                                data.percent = item.percent;
                                await adb.InsertOrReplaceAsync(data);
                            }
                        }

                        //update last time checked for badge progress
                        string settingsKey = $"BadgeProgressDate-{GlobalResources.GetUserEmail()}";
                        dbSettings sBadgeProgressSettings = adb.Table<dbSettings>().Where(x => x.Key == settingsKey).FirstOrDefaultAsync().Result;
                        if (sBadgeProgressSettings == null)
                        {
                            sBadgeProgressSettings = new dbSettings() { Key = settingsKey };
                        }
                        //Update date last time checked for badges
                        try
                        {
                            sBadgeProgressSettings.Value = DateTime.UtcNow.ToString();
                            await adb.InsertOrReplaceAsync(sBadgeProgressSettings);
                        }
                        catch (Exception)
                        {
                            sBadgeProgressSettings.Value = DateTime.UtcNow.ToString();
                            await adb.InsertOrReplaceAsync(sBadgeProgressSettings);
                        }
                    }

                }
                //Progress was made, show popup if 100 percent achieved
                else if (root.payload?.data?.progressUpdated?.progress != null)
                {
                    DabGraphQlProgress progress = new DabGraphQlProgress(root.payload.data.progressUpdated.progress);
                    if (progress.percent == 100 && (progress.seen == null || progress.seen == false))
                    {
                        //log to firebase
                        var fbInfo = new Dictionary<string, string>();
                        fbInfo.Add("user", GlobalResources.GetUserEmail());
                        fbInfo.Add("idiom", Device.Idiom.ToString());
                        fbInfo.Add("badgeId", progress.badgeId.ToString());
                        DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_progressAchieved", fbInfo);


                        await PopupNavigation.Instance.PushAsync(new AchievementsProgressPopup(progress));
                        progress.seen = true;
                    }
                    dbUserBadgeProgress newProgress = new dbUserBadgeProgress(progress, userName);

                    dbUserBadgeProgress data = adb.Table<dbUserBadgeProgress>().Where(x => x.id == newProgress.id && x.userName == userName).FirstOrDefaultAsync().Result;
                    try
                    {
                        if (data == null)
                        {
                            await adb.InsertOrReplaceAsync(newProgress);
                        }
                        else
                        {
                            data.percent = newProgress.percent;
                            await adb.InsertOrReplaceAsync(data);
                        }
                    }
                    catch (Exception)
                    {
                        if (data == null)
                        {
                            await adb.InsertOrReplaceAsync(newProgress);
                        }
                        else
                        {
                            data.percent = newProgress.percent;
                            await adb.InsertOrReplaceAsync(data);
                        }
                    }
                }
                else if (root.payload?.data?.updateUser?.user != null)
                {
                    GraphQlUser user = new GraphQlUser(root.payload.data.updateUser.user);

                    dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                    dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                    dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;

                    EmailSettings.Value = user.email;
                    FirstNameSettings.Value = user.firstName;
                    LastNameSettings.Value = user.lastName;
                    dbSettings.StoreSetting("Channel", user.channel);
                    dbSettings.StoreSetting("Channels", user.channels);
                    dbSettings.StoreSetting("Language", user.language);
                    dbSettings.StoreSetting("NickName", user.nickname);

                    await adb.UpdateAsync(EmailSettings);
                    await adb.UpdateAsync(FirstNameSettings);
                    await adb.UpdateAsync(LastNameSettings);

                }
                else if (root.payload?.data?.user != null)
                {
                    //We got back user data!
                    GraphQlLoginComplete = true; //stop processing success messages.
                                                 //Save the data

                    var user = root.payload.data.user;

                    var avatarValue = "https://www.gravatar.com/avatar/" + CalculateMD5Hash(GlobalResources.GetUserEmail()) + "?d=mp";

                    dbSettings.StoreSetting("Avatar", avatarValue);
                    dbSettings.StoreSetting("WpId", user.wpId.ToString());
                    dbSettings.StoreSetting("Email", user.email);
                    dbSettings.StoreSetting("Channel", user.channel);
                    dbSettings.StoreSetting("Channels", user.channels);
                    dbSettings.StoreSetting("FirstName", user.firstName);
                    dbSettings.StoreSetting("LastName", user.lastName);
                    dbSettings.StoreSetting("Language", user.language);
                    dbSettings.StoreSetting("NickName", user.nickname);

                    GraphQlLoginRequestInProgress = false;

                    GuestStatus.Current.IsGuestLogin = false;
                    await AuthenticationAPI.GetMemberData();

                    //Disconnect

                    //user is logged in
                    GlobalResources.WaitStop();
                    GlobalResources.Instance.IsLoggedIn = true;

                    GuestStatus.Current.UserName = GlobalResources.GetUserName();

                    DabChannelsPage _nav = new DabChannelsPage();
                    _nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage = new NavigationPage(_nav);
                    });

                    //await Navigation.PushAsync(_nav);
                    MessagingCenter.Send<string>("Setup", "Setup");
                }
                else if (root.payload?.data?.updateUserFields != null)
                {
                    DabGraphQlUpdateUserFields fields = root.payload.data.updateUserFields;
                    
                    dbSettings.StoreSetting("Email", fields.email);
                    dbSettings.StoreSetting("FirstName", fields.firstName);
                    dbSettings.StoreSetting("LastName", fields.lastName);

                    GlobalResources.WaitStop();
                    var UserName = GlobalResources.GetUserName().Split(' ');
                    GuestStatus.Current.UserName = GlobalResources.GetUserName();
                    Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.DisplayAlert("Success", "User profile information has been updated", "OK"); ; });
                    if (popRequests < 1)
                    {
                        popRequests = popRequests + 1;
                        Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.Navigation.PopAsync(); });
                    }
                    else
                        popRequests = 0;
                }
                else if (root.payload?.data?.updatePassword != null)
                {
                    GlobalResources.WaitStop();
                    if (root.payload.data.updatePassword == true)
                    {
                        Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.DisplayAlert("Success", "Your password has been updated", "OK"); ; });
                        if (popRequests < 2)
                        {
                            popRequests = popRequests + 1;
                            Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.Navigation.PopAsync(); });
                        }
                        else
                            popRequests = 1;
                    }
                }
                if (root?.payload?.data?.registerUser != null)
                {
                    try
                    {
                        var user = root.payload.data.registerUser;
                        //Store the token
                        dbSettings sToken = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                        if (sToken == null)
                        {
                            sToken = new dbSettings() { Key = "Token" };
                        }
                        sToken.Value = user.token;
                        await adb.InsertOrReplaceAsync(sToken);

                        //Update Token Life
                        ContentConfig.Instance.options.token_life = 5;
                        dbSettings sTokenCreationDate = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                        if (sTokenCreationDate == null)
                        {
                            sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
                        }
                        sTokenCreationDate.Value = DateTime.Now.ToString();
                        await adb.InsertOrReplaceAsync(sTokenCreationDate);

                        //Reset the connection with the new token
                        //DabSyncService.Instance.DisconnectGraphQl(true);
                        DabSyncService.Instance.ConnectGraphQl(sToken.Value);

                        //Send a request for updated user data

                        string jUser = $"query {{user{{wpId,firstName,lastName,email}}}}";
                        var pLogin = new DabGraphQlPayload(jUser, new DabGraphQlVariables());
                        DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", pLogin)));
                    }
                    catch (Exception ex)
                    {
                        GlobalResources.WaitStop();
                        Debug.WriteLine(ex.Message);
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Application.Current.MainPage.DisplayAlert("System Error", "System error with login. Try again or restart application.", "Ok");
                            Application.Current.MainPage.Navigation.PushAsync(new DabCheckEmailPage());
                        });
                        
                    }
                }

            }
            catch (Exception ex)
            {
                //log error to firebase
                var errorInfo = new Dictionary<string, string>();
                errorInfo.Add("user", GlobalResources.GetUserEmail());
                errorInfo.Add("idiom", Device.Idiom.ToString());
                errorInfo.Add("error", $"Exception-Caught: {ex.ToString()}");
                DependencyService.Get<IAnalyticsService>().LogEvent("websocket_graphql_error", errorInfo);


                System.Diagnostics.Debug.WriteLine("Error in MessageReceived: " + ex.ToString());
                GlobalResources.WaitStop();
            }
        }

        public string CalculateMD5Hash(string email)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(email);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public bool ConnectWebsocket()
        {
            //This method initializes and connects the websocket connection itself

            GlobalResources.WaitStart("Connecting to the Daily Audio Bible servers...");

            if (!IsConnected)
            {
                //Set up the socket and connect it so it can be used throughout the app.

                //Create socket
                sock = DependencyService.Get<IWebSocket>(DependencyFetchTarget.NewInstance);

                //Get the URL to use
                var appSettings = ContentConfig.Instance.app_settings;
                string uri = (GlobalResources.TestMode) ? appSettings.stage_service_link : appSettings.prod_service_link;
                //need to add wss:// since it just gives us the address here
                uri = $"wss://{uri}";

                //Register for notifications from the socket
                sock.DabSocketEvent += Sock_DabSocketEvent;
                sock.DabGraphQlMessage += Sock_DabGraphQlMessage;

                //Init the socket
                sock.Init(uri);

                //Connect the socke
                sock.Connect();

                return true;
            }
            else
            {
                Debug.WriteLine("Socket already connected. Will not connect again again.");
                return false;
            }

        }

        public void DisconnectWebSocket(bool LogOutUser)
            //Disconnects the websocket (will also disconnect graphql)
        {
            if (IsConnected)
            {
                //Disconnect GraphQL first
                DisconnectGraphQl(LogOutUser);

                //Disconnect the socket
                sock.Disconnect();
            }
        }

        public async void DisconnectGraphQl(bool LogOutUser)
            //Terminates the connection to GraphQL

        {
            //Unsubscribe from all subscriptions
            foreach (int id in subscriptionIds)
            {
                var jSub = $"{{\"type\":\"stop\",\"id\":\"{id}\",\"payload\":\"null\"}}";
                sock.Send(jSub);
            }
            subscriptionIds.Clear();

            //Log the user out, if requested and they are logged in.
            if (LogOutUser)
            {
                if (!GuestStatus.Current.IsGuestLogin)
                {
                    var jLogout = "{\"type\":\"start\",\"payload\":{\"query\":\"mutation {logoutUser(version: 1)}\",\"variables\":{}}}";
                    Send(jLogout);
                }
            }

            //Terminate the connection before disconnecting it.
            var jTerm = "{\"type\":\"connection_terminate\"}";
            sock.Send(jTerm);
        }

        public void Send(string JsonIn)
        {
            sock.Send(JsonIn);
        }

        private void Sock_DabSocketEvent(object sender, DabSocketEventHandler e)
        {
            //An event has been fired by the socket. Respond accordingly

            //Log the event to the debugger
            Debug.WriteLine($"{e.eventName} was fired with {e.data}");

            //Take action on the event
            switch (e.eventName.ToLower())
            {
                case "disconnected": //Socket disconnected
                    Sock_Disconnected(e.data);
                    break;
                case "connected": //Socket connected
                    Sock_Connected(e.data);
                    break;
                case "reconnecting": //Socket reconnecting
                    //do nothing for now
                    break;
                case "reconnected": //Socket reconnected
                    Sock_Connected(e.data);
                    break;
                case "auth_error": //Error with authentication
                    Sock_ErrorOccured(e.eventName, e.data);
                    break;
                default:
                    break;
            }
        }

        //IsConnected returns a bool indicating whether the socket is currently connected.
        //This is a bindable property
        public bool IsConnected
        {
            get
            {
                return sock == null ? false : sock.IsConnected;
            }
        }

        //Opposite of IsConnected used for binding reasons.
        public bool IsDisconnected
        {
            get
            {
                return sock == null ? true : !sock.IsConnected;
            }

        }

        private void Sock_Disconnected(string data)
        {
            //The socket got disconnected.

            //Notify UI
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsDisconnected");
            if (!sock.IsConnected)
            {
                GlobalResources.WaitStop();
            }
        }

        private void Sock_ErrorOccured(string eventName, object data)
        {
            //The socket has encountenered an error. Take appropriate action.

            //For now, disconnect and then try to reconnect
            if (sock.IsConnected)
            {
                sock.Disconnect();
                sock.Connect();
            }

            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsDisconnected");
        }

        public void ConnectGraphQl(string Token)
        {
            string origin;
            if (Device.RuntimePlatform == Device.Android)
            {
                origin = "c2it-android";
            }
            else if (Device.RuntimePlatform == Device.iOS)
            {
                origin = "c2it-ios";
            }
            else
            {
                origin = "could not determine runtime platform";
            }

            Payload token = new Payload(Token, origin);
            var ConnectInit = JsonConvert.SerializeObject(new ConnectionInitSyncSocket("connection_init", token));
            sock.Send(ConnectInit);


        }

        private void Sock_Connected(object data)
        {
            //The socket has connected or reconnected. Take appropriate action
            GlobalResources.WaitStop();
            //Notify UI
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsDisconnected");
            dbSettings Token = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
            if (GlobalResources.TestMode && !GlobalResources.Instance.IsLoggedIn)
            {
                Token = new dbSettings() { Key = "Token", Value = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczpcL1wvc3RhZ2luZy5kYWlseWF1ZGlvYmlibGUuY29tIiwiaWF0IjoxNTgyOTEwMTI1LCJuYmYiOjE1ODI5MTAxMjUsImV4cCI6MTc0MDU5MDEyNSwiZGF0YSI6eyJ1c2VyIjp7ImlkIjoiMTI5MTcifX19.bT-Bnn6SdHc4rKQ37vMjrllUeKbsvdvMUJ0pBzMy8Fs" }; //test mode token
            }
            if (Token == null) Token = new dbSettings() { Key = "Token", Value = GlobalResources.APIKey }; //fake token
            //Init the GraphQL connection
            ConnectGraphQl(Token.Value);
            //Only send user based subscriptions when user is logged in
            if (GuestStatus.Current.IsGuestLogin == false && GlobalResources.Instance.IsLoggedIn)
            {
                //SUBSCRIPTION 1 - ACTION LOGGED
                var query = "subscription { actionLogged { action { id userId episodeId listen position favorite entryDate updatedAt createdAt } } }";
                DabGraphQlPayload payload = new DabGraphQlPayload(query, variables);
                var SubscriptionInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", payload, 1));
                subscriptionIds.Add(1);
                sock.Send(SubscriptionInit);

                //SUBSCRIPTION 2 - TOKEN REMOVED
                var tokenRemovedQuery = "subscription { tokenRemoved { token } }";
                DabGraphQlPayload tokenRemovedPayload = new DabGraphQlPayload(tokenRemovedQuery, variables);
                var SubscriptionRemoveToken = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", tokenRemovedPayload, 2));
                subscriptionIds.Add(2);
                sock.Send(SubscriptionRemoveToken);

                //SUBSCRIPTION 3 - PROGRESS UPDATED
                var newProgressQuery = "subscription { progressUpdated { progress { id badgeId percent year seen createdAt updatedAt } } }";
                DabGraphQlPayload newProgressPayload = new DabGraphQlPayload(newProgressQuery, variables);
                var SubscriptionProgressData = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newProgressPayload, 3));
                subscriptionIds.Add(3);
                sock.Send(SubscriptionProgressData);

                //SUBSCRIPTION 4 - USER UPDATED
                var userUpdatedQuery = "subscription { updateUser { user { id wpId firstName lastName email language } } } ";
                DabGraphQlPayload userUpdatedPayload = new DabGraphQlPayload(userUpdatedQuery, variables);
                var SubscriptionUpdatedUser = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", userUpdatedPayload, 6));
                subscriptionIds.Add(6);
                sock.Send(SubscriptionUpdatedUser);

                ////SUBSCRIPTION 5 - USER ADDRESS UPDATED
                //var userAddressQuery = "subscription { updateUser { user { id wpId firstName lastName email language } } } ";
                //DabGraphQlPayload userAddressPayload = new DabGraphQlPayload(userAddressQuery, variables);
                //var SubscriptionUpdatedUserAddress = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", userAddressPayload, 7));
                //subscriptionIds.Add(7);
                //sock.Send(SubscriptionUpdatedUserAddress);

                //QUERY - RECENT PROGRESS
                var badgeProgressQuery = "query { updatedProgress(date: \"" + GlobalResources.BadgeProgressUpdatesDate.ToString("o") + "Z\") { edges { id badgeId percent seen year createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                DabGraphQlPayload newBadgeProgressPayload = new DabGraphQlPayload(badgeProgressQuery, variables);
                var progressInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newBadgeProgressPayload, 0));
                sock.Send(progressInit);

                //get recent actions when we get a connection made
                var gmd = AuthenticationAPI.GetMemberData().Result;
            }

            //SUBSCRIPTION 4 - EPISODE PUBLISHED
            var newEpisodeQuery = "subscription { episodePublished { episode { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } } }";
            DabGraphQlPayload newEpisodePayload = new DabGraphQlPayload(newEpisodeQuery, variables);
            var SubscriptionNewEpisode = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newEpisodePayload, 4));
            subscriptionIds.Add(4);
            sock.Send(SubscriptionNewEpisode);


            //SUBSCRIPTION 5 - BADGE UPDATED
            var newBadgeQuery = "subscription { badgeUpdated { badge { badgeId name description imageURL type method data visible createdAt updatedAt } } }";
            DabGraphQlPayload newBadgePayload = new DabGraphQlPayload(newBadgeQuery, variables);
            var SubscriptionBadgeData = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newBadgePayload, 5));
            subscriptionIds.Add(5);
            sock.Send(SubscriptionBadgeData);

            //QUERY - CHANNELS
            var channelQuery = "query { channels { id channelId key title imageURL rolloverMonth rolloverDay bufferPeriod bufferLength public createdAt updatedAt}}";
            DabGraphQlPayload channelPayload = new DabGraphQlPayload(channelQuery, variables);
            var channelInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", channelPayload, 0));
            sock.Send(channelInit);


            //QUERY - UPDATED BADDGES
            var updatedBadgesQuery = "query { updatedBadges(date: \"" + GlobalResources.BadgesUpdatedDate.ToString("o") + "Z\") { edges { badgeId id name description imageURL type method visible createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
            DabGraphQlPayload newBadgeUpdatePayload = new DabGraphQlPayload(updatedBadgesQuery, variables);
            var badgeInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newBadgeUpdatePayload, 0));
            sock.Send(badgeInit);

        }

        /* Events to handle Binding */
        public virtual void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
