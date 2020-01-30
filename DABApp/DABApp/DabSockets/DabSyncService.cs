using DABApp.ChannelWebSocketHelper;
using DABApp.LastActionsHelper;
using DABApp.LoggedActionHelper;
using DABApp.WebSocketHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

        SQLiteConnection db = DabData.database;
        SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

        string origin;

        int channelId;
        dbChannels channel = new dbChannels();
        List<DabGraphQlEpisode> allEpisodes = new List<DabGraphQlEpisode>();



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

            try
            {
                var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                //Action logged elsewhere
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
                            MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                        }

                        //Send last action query to the websocket
                        //TODO: Come back and clean up with GraphQl objects
                        Variables variables = new Variables();
                        System.Diagnostics.Debug.WriteLine($"Getting actions since {GlobalResources.LastActionDate.ToString()}...");
                        var updateEpisodesQuery = "{ lastActions(date: \"" + GlobalResources.LastActionDate.ToString("o") + "Z\", cursor: \"" + root.payload.data.lastActions.pageInfo.endCursor + "\") { edges { id episodeId userId favorite listen position entryDate updatedAt createdAt } pageInfo { hasNextPage endCursor } } } ";
                        var updateEpisodesPayload = new WebSocketHelper.Payload(updateEpisodesQuery, variables);
                        var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", updateEpisodesPayload));
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
                //else if (e.Message.Contains("actions")) //Should no longer be needed since we store user episode meta
                //{
                //    //process incoming new episode data
                //    List<LastActionsHelper.Edge> actionsList = new List<LastActionsHelper.Edge>();  //list of actions
                //    ActionsRootObject actionsObject = JsonConvert.DeserializeObject<ActionsRootObject>(e.Message);
                //    if (actionsObject.payload.data.actions != null) //make sure we got somethign back
                //    {
                //        System.Diagnostics.Debug.WriteLine($"Received {actionsObject.payload.data.actions.edges.Count} actions...");
                //        foreach (LastActionsHelper.Edge item in actionsObject.payload.data.actions.edges.OrderByDescending(x => x.createdAt))//loop throgh them all in most recent order first and update episode data (without sending episode changed messages)
                //        {
                //            await PlayerFeedAPI.UpdateEpisodeProperty(item.episodeId, item.listen, item.favorite, item.hasJournal, item.position, false);

                //        }
                //        MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged"); //tell listeners episodes have changed.
                //    }
                //}
                else if (root.payload?.data?.episodes != null)
                {
                    //var existingEpisodes = db.Table<dbEpisodes>().Where(x => x.id == 227).ToList();
                    //LastEpisodeDateQueryHelper.LastEpisodeQueryRootObject episodesObject = JsonConvert.DeserializeObject<LastEpisodeDateQueryHelper.LastEpisodeQueryRootObject>(e.Message);

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
                        Variables variables = new Variables();
                        Debug.WriteLine($"Getting episodes by ChannelId");
                        var episodesByChannelQuery = "query { episodes(date: \"" + lastEpisodeQueryDate + "\", channelId: " + channelId + ", cursor: \"" + root.payload.data.episodes.pageInfo.endCursor + "\") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                        var episodesByChannelPayload = new WebSocketHelper.Payload(episodesByChannelQuery, variables);
                        var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", episodesByChannelPayload));
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
                            //do something
                        }
                    }

                    //store a new episode query date
                    GlobalResources.SetLastEpisodeQueryDate(channelId);
                }
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

        public void Disconnect()
        {
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

        private void Sock_Connected(object data)
        {
            //The socket has connected or reconnected. Take appropriate action

            //Notify UI
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsDisconnected");
            dbSettings Token = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
            if (Token != null) //if user hasn't logged in this may not be valid.
            {
                if(Device.RuntimePlatform == Device.Android)
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
                Payload token = new Payload(Token.Value, origin);
                var ConnectInit = JsonConvert.SerializeObject(new ConnectionInitSyncSocket("connection_init", token));
                sock.Send(ConnectInit);

                //Subscribe for action logs
                var variables = new Variables();
                var query = "subscription {\n actionLogged {\n action {\n userId\n episodeId\n listen\n position\n favorite\n entryDate\n }\n }\n }";
                WebSocketHelper.Payload payload = new WebSocketHelper.Payload(query, variables);
                var SubscriptionInit = JsonConvert.SerializeObject(new WebSocketSubscription("start", payload));
                sock.Send(SubscriptionInit);

                //Send request for channels lists
                var channelVariables = new Variables();
                var channelQuery = "query { channels { id channelId key title imageURL rolloverMonth rolloverDay bufferPeriod bufferLength public createdAt updatedAt}}";
                WebSocketHelper.Payload channelPayload = new WebSocketHelper.Payload(channelQuery, channelVariables);
                var channelInit = JsonConvert.SerializeObject(new WebSocketCommunication("start", channelPayload));
                sock.Send(channelInit);

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
