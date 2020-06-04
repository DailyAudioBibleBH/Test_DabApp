﻿using DABApp.DabUI;
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


        private DabSyncService()
        {
            //Constructure is private so we only allow one of them
        }

        public bool Init()
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

            return true;
        }

        private async void Sock_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            Debug.WriteLine($"Shared code graph ql message: {e.Message}");

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
                    //DabSyncService.Instance.Disconnect(false);
                    //DabSyncService.Instance.Connect();
                    //Reset the connection with the new token
                    DabSyncService.Instance.PrepConnectionWithTokenAndOrigin(sToken.Value);

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

                            dbSettings ChannelSettings = adb.Table<dbSettings>().Where(x => x.Key == "Channel").FirstOrDefaultAsync().Result;
                            if (ChannelSettings != null)
                            {
                                dbChannels favChannel = adb.Table<dbChannels>().Where(x => x.title == ChannelSettings.Value).FirstOrDefaultAsync().Result;
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

                    Instance.Init();
                    Instance.Connect();
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
                    dbSettings LanguageSettings = adb.Table<dbSettings>().Where(x => x.Key == "Language").FirstOrDefaultAsync().Result;
                    dbSettings ChannelSettings = adb.Table<dbSettings>().Where(x => x.Key == "Channel").FirstOrDefaultAsync().Result;
                    dbSettings ChannelsSettings = adb.Table<dbSettings>().Where(x => x.Key == "Channels").FirstOrDefaultAsync().Result;
                    dbSettings NickNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "NickName").FirstOrDefaultAsync().Result;
                    if (LanguageSettings == null)
                    {
                        LanguageSettings = new dbSettings() { Key = "Language" };
                    }
                    if (NickNameSettings == null)
                    {
                        NickNameSettings = new dbSettings() { Key = "NickName" };
                    }
                    if (ChannelSettings == null)
                    {
                        ChannelSettings = new dbSettings() { Key = "Channel" };
                    }
                    if (ChannelsSettings == null)
                    {
                        ChannelsSettings = new dbSettings() { Key = "Channels" };
                    }

                    EmailSettings.Value = user.email;
                    FirstNameSettings.Value = user.firstName;
                    LastNameSettings.Value = user.lastName;
                    LanguageSettings.Value = user.language;
                    ChannelSettings.Value = user.channel;
                    ChannelsSettings.Value = user.channels;
                    NickNameSettings.Value = user.nickname;

                    await adb.UpdateAsync(EmailSettings);
                    await adb.UpdateAsync(FirstNameSettings);
                    await adb.UpdateAsync(LastNameSettings);
                    await adb.UpdateAsync(LanguageSettings);
                    await adb.UpdateAsync(ChannelSettings);
                    await adb.UpdateAsync(ChannelsSettings);
                    await adb.UpdateAsync(NickNameSettings);

                }
                else if (root.payload?.data?.user != null)
                {
                    var user = root.payload.data.user;
                    if (user.email != null)
                    {
                        dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                        if (EmailSettings == null)
                        {
                            EmailSettings = new dbSettings() { Key = "Email" };
                        }
                        EmailSettings.Value = user.email;
                        await adb.InsertOrReplaceAsync(EmailSettings);

                    }
                    if (user.channel != null)
                    {
                        //Find out how we want to do this
                        dbSettings ChannelSettings = adb.Table<dbSettings>().Where(x => x.Key == "Channel").FirstOrDefaultAsync().Result;
                        if (ChannelSettings == null)
                        {
                            ChannelSettings = new dbSettings() { Key = "Channel" };
                        }
                        ChannelSettings.Value = user.channel;
                        await adb.InsertOrReplaceAsync(ChannelSettings);
                    }
                    if (user.channels != null)
                    {
                        //Find out how we want to do this
                        dbSettings ChannelsSettings = adb.Table<dbSettings>().Where(x => x.Key == "Channels").FirstOrDefaultAsync().Result;
                        if (ChannelsSettings == null)
                        {
                            ChannelsSettings = new dbSettings() { Key = "Channels" };
                        }
                        ChannelsSettings.Value = user.channels.ToString();
                        await adb.InsertOrReplaceAsync(ChannelsSettings);
                    }
                    if (user.firstName != null)
                    {
                        dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                        if (FirstNameSettings == null)
                        {
                            FirstNameSettings = new dbSettings() { Key = "FirstName" };
                        }
                        FirstNameSettings.Value = user.firstName;
                        await adb.InsertOrReplaceAsync(FirstNameSettings);
                    }
                    if (user.lastName != null)
                    {
                        dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
                        if (LastNameSettings == null)
                        {
                            LastNameSettings = new dbSettings() { Key = "LastName" };
                        }
                        LastNameSettings.Value = user.lastName;
                        await adb.InsertOrReplaceAsync(LastNameSettings);
                    }
                    if (user.language != null)
                    {
                        dbSettings LanguageSettings = adb.Table<dbSettings>().Where(x => x.Key == "Language").FirstOrDefaultAsync().Result;
                        if (LanguageSettings == null)
                        {
                            LanguageSettings = new dbSettings() { Key = "Language" };
                        }
                        LanguageSettings.Value = user.language;
                        await adb.InsertOrReplaceAsync(LanguageSettings);
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

        public void Connect()
        {
            GlobalResources.WaitStart("Connecting to the Daily Audio Bible servers...");
            sock.Connect();

            var current = Connectivity.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                // Connection to internet is not available
                Device.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Error trying to connect to the websocket, please check your internet connection", "OK");

                });
            }
        }

        public void Disconnect(bool LogOutUser)
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

            //Disconnect the socket
            sock.Disconnect();
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

        public void PrepConnectionWithTokenAndOrigin(string Token)
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
            //Init the connection
            PrepConnectionWithTokenAndOrigin(Token.Value);
            //Only send user based subscriptions when user is logged in
            if (GuestStatus.Current.IsGuestLogin == false)
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

                //SUBSCRIPTION 5 - USER ADDRESS UPDATED
                var userAddressQuery = "subscription { updateUser { user { id wpId firstName lastName email language } } } ";
                DabGraphQlPayload userAddressPayload = new DabGraphQlPayload(userAddressQuery, variables);
                var SubscriptionUpdatedUserAddress = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", userAddressPayload, 7));
                subscriptionIds.Add(7);
                sock.Send(SubscriptionUpdatedUserAddress);

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
