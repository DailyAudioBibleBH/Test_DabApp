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

        SQLiteConnection db = DabData.database;
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

            foreach (var item in ContentConfig.Instance.views)
            {
                System.Diagnostics.Debug.WriteLine(item.title);
            }

            try
            {
                var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                //Generic keep alive
                if (root.type=="ka")
                {
                    //Nothing to see here...
                }
                //Action logged elsewhere
                else if (root.payload?.data?.actionLogged != null)
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
                    }
                }
                else if (root.payload?.data?.episodes != null)
                {
                    MessagingCenter.Send<string>("WaitUI", "StartWaitUI");
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
                        var episodesByChannelQuery = "query { episodes(date: \"" + lastEpisodeQueryDate + "\", channelId: " + channelId + ", cursor: \"" + root.payload.data.episodes.pageInfo.endCursor + "\") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                        var episodesByChannelPayload = new DabGraphQlPayload(episodesByChannelQuery, variables);
                        var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", episodesByChannelPayload));
                        DabSyncService.Instance.Send(JsonIn);
                    }
                    else {
                        //Last page, let UI know
                        var channels = db.Table<dbChannels>().OrderByDescending(x => x.channelId);
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
                            MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                        }
                        MessagingCenter.Send<string>("WaitUI", "StopWaitUI");
                    }

                    //store a new episode query date
                    GlobalResources.SetLastEpisodeQueryDate(channelId);
                    //stop wait ui on episodes page
                }
                else if (root.payload?.data?.triggerEpisodeSubscription != null)
                {
                    dbEpisodes newEpisode = new dbEpisodes(root.payload.data.triggerEpisodeSubscription);
                    await adb.InsertAsync(newEpisode);
                    MessagingCenter.Send<string>("Update", "Update");
                    await PlayerFeedAPI.DownloadEpisodes();
                }
                else if (root.payload?.data?.tokenRemoved?.token != null)
                {
                    //Expire the token (should log the user out?)
                    dbSettings sTokenCreationDate = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenCreation");
                    if (sTokenCreationDate == null)
                    {
                        sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
                    }
                    sTokenCreationDate.Value = DateTime.Now.AddDays(5).ToString();
                    db.Update(sTokenCreationDate);
                    sock.Disconnect();
                    Device.BeginInvokeOnMainThread(() => { MessagingCenter.Send<string>("Logout", "Logout"); });
                    Debug.WriteLine($"SOCKET jwt_expired {DateTime.Now}");
                }
                else if (root.payload?.data?.updateToken?.token != null)
                {
                    //Update Token
                    dbSettings sToken = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                    if (sToken == null)
                    {
                        sToken = new dbSettings() { Key = "Token" };
                    }
                    sToken.Value = root.payload.data.updateToken.token;
                    await adb.UpdateAsync(sToken);

                    //Update Token Life
                    dbSettings sTokenCreationDate = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenCreation");
                    if (sTokenCreationDate == null)
                    {
                        sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
                    }
                    sTokenCreationDate.Value = DateTime.Now.ToString();
                    db.InsertOrReplace(sTokenCreationDate);

                    Instance.Init();
                    Instance.Connect();
                }
                else if (root.payload?.data?.updatedBadges != null)
                {
                    Device.InvokeOnMainThreadAsync(async () =>
                    {
                        if (root.payload?.data?.updatedBadges.edges.Count() > 0)
                        {
                            //add badges to db
                            foreach (var item in root.payload.data.updatedBadges.edges)
                            {
                                await adb.InsertOrReplaceAsync(item);
                            };
                        }

                        dbSettings sBadgeUpdateSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "BadgeUpdateDate");
                        if (sBadgeUpdateSettings == null)
                        {
                            sBadgeUpdateSettings = new dbSettings() { Key = "BadgeUpdateDate" };
                        }
                        //Update date last time checked for badges
                        sBadgeUpdateSettings.Value = DateTime.UtcNow.ToString();
                        db.InsertOrReplace(sBadgeUpdateSettings);
                    });
                    
                }
                else if (root.payload?.data?.updatedProgress != null)
                {
                    foreach (var item in root.payload.data.updatedProgress.edges)
                    {
                        dbUserBadgeProgress data = db.Table<dbUserBadgeProgress>().SingleOrDefault(x => x.id == item.id && x.userName == userName);
                        Device.InvokeOnMainThreadAsync(async () =>
                        {
                            if (data == null)
                            {
                                await adb.InsertOrReplaceAsync(item);
                            }
                            else
                            {
                                data.percent = item.percent;
                                await adb.InsertOrReplaceAsync(data);
                            }

                            string settingsKey = $"BadgeProgressDate-{GlobalResources.GetUserEmail()}";
                            dbSettings sBadgeProgressSettings = db.Table<dbSettings>().SingleOrDefault(x => x.Key == settingsKey);
                            if (sBadgeProgressSettings == null)
                            {
                                sBadgeProgressSettings = new dbSettings() { Key = settingsKey };
                            }
                            //Update date last time checked for badges
                            sBadgeProgressSettings.Value = DateTime.UtcNow.ToString();
                            db.InsertOrReplace(sBadgeProgressSettings);
                        });
                        
                    }
                    
                }
                else if (root.payload?.data?.progressUpdated?.progress != null)
                {
                    DabGraphQlProgress progress = new DabGraphQlProgress(root.payload.data.progressUpdated.progress);
                    if (progress.percent == 100 && (progress.seen == null || progress.seen == false))
                    {
                        await PopupNavigation.PushAsync(new AchievementsProgressPopup(progress));
                        progress.seen = true;
                    }
                    dbUserBadgeProgress newProgress = new dbUserBadgeProgress(progress, userName);
                    
                    dbUserBadgeProgress data = db.Table<dbUserBadgeProgress>().SingleOrDefault(x => x.id == newProgress.id && x.userName == userName);
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
                            db.InsertOrReplace(newProgress);
                        }
                        else
                        {
                            data.percent = newProgress.percent;
                            db.InsertOrReplace(data);
                        }
                    }
                    
                }
                var test = db.Table<dbUserBadgeProgress>().ToList();
                var test2 = db.Table<Badge>().ToList();
                var breakpoint = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in MessageReceived: " + ex.ToString());
            }
        }

        public void Connect()
        {
            sock.Connect();
        }

        public void Disconnect(bool LogOutUser)
        {


            //Unsubscribe from all subscriptions
            foreach(int id in subscriptionIds)
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

            //Notify UI
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsDisconnected");
            dbSettings Token = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
            if (Token != null) //if user hasn't logged in this may not be valid.
            {

                //Init the connection
                PrepConnectionWithTokenAndOrigin(Token.Value);

                //Subscribe to action logs - SUB 1
                var query = "subscription { actionLogged { action { id userId episodeId listen position favorite entryDate updatedAt createdAt } } }";
                DabGraphQlPayload payload = new DabGraphQlPayload(query, variables);
                var SubscriptionInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", payload,1));
                subscriptionIds.Add(1);
                sock.Send(SubscriptionInit);

                //Subscribe to token removed/forceful logout - SUB 2
                var tokenRemovedQuery = "subscription { tokenRemoved { token } }";
                DabGraphQlPayload tokenRemovedPayload = new DabGraphQlPayload(tokenRemovedQuery, variables);
                var SubscriptionRemoveToken = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", tokenRemovedPayload,2));
                subscriptionIds.Add(2);
                sock.Send(SubscriptionRemoveToken);

                //Subscribe for new episodes SUB 3
                var newEpisodeQuery = "subscription { episodePublished { episode { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } } }";
                DabGraphQlPayload newEpisodePayload = new DabGraphQlPayload(newEpisodeQuery, variables);
                var SubscriptionNewEpisode = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newEpisodePayload,3));
                subscriptionIds.Add(3);
                sock.Send(SubscriptionNewEpisode);

                //Send request for channels lists - SUB 4
                var channelQuery = "query { channels { id channelId key title imageURL rolloverMonth rolloverDay bufferPeriod bufferLength public createdAt updatedAt}}";
                DabGraphQlPayload channelPayload = new DabGraphQlPayload(channelQuery, variables);
                var channelInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", channelPayload,4));
                subscriptionIds.Add(4);
                sock.Send(channelInit);

                //Subscribe to badge data SUB 5
                var newBadgeQuery = "subscription { badgeUpdated { badge { badgeId name description imageURL type method data visible createdAt updatedAt } } }";
                DabGraphQlPayload newBadgePayload = new DabGraphQlPayload(newBadgeQuery, variables);
                var SubscriptionBadgeData = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newBadgePayload, 5));
                subscriptionIds.Add(5);
                sock.Send(SubscriptionBadgeData);

                //Subscribe to progress data SUB 6
                var newProgressQuery = "subscription { progressUpdated { progress { id badgeId percent year seen createdAt updatedAt } } }";
                DabGraphQlPayload newProgressPayload = new DabGraphQlPayload(newProgressQuery, variables);
                var SubscriptionProgressData = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newProgressPayload, 6));
                subscriptionIds.Add(6);
                sock.Send(SubscriptionProgressData);

                //Send request for all badges since given date
                var updatedBadgesQuery = "query { updatedBadges(date: \"" + GlobalResources.BadgesUpdatedDate.ToString("o") + "Z\") { edges { badgeId id name description imageURL type method data visible createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                DabGraphQlPayload newBadgeUpdatePayload = new DabGraphQlPayload(updatedBadgesQuery, variables);
                var badgeInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newBadgeUpdatePayload, 7));
                subscriptionIds.Add(7);
                sock.Send(badgeInit);

                //Send request for user badge progress since given date
                var badgeProgressQuery = "query { updatedProgress(date: \"" + GlobalResources.BadgeProgressUpdatesDate.ToString("o") + "Z\") { edges { id badgeId percent data seen year createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                DabGraphQlPayload newBadgeProgressPayload = new DabGraphQlPayload(badgeProgressQuery, variables);
                var progressInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", newBadgeProgressPayload, 8));
                subscriptionIds.Add(8);
                sock.Send(progressInit);

                //get recent actions when we get a connection made
                var gmd = AuthenticationAPI.GetMemberData().Result;

            }
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
