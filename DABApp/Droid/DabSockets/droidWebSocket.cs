using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.Droid.DabSockets;
using Xamarin.Forms;
using DABApp.DabSockets;
using WebSocket4Net;
using Newtonsoft.Json;
using DABApp.LoggedActionHelper;
using DABApp.WebSocketHelper;
using DABApp.LastActionsHelper;
using Edge = DABApp.LastActionsHelper.Edge;
using SQLite;
using DABApp.LastEpisodeDateQueryHelper;

[assembly: Dependency(typeof(droidWebSocket))]
namespace DABApp.Droid.DabSockets
{
    public class droidWebSocket : IWebSocket
    {
        bool isConnected = false;
        WebSocket4Net.WebSocket sock;
        public event EventHandler<DabSocketEventHandler> DabSocketEvent;
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


        public droidWebSocket()
        {
        }



        //Returns current connection state of the socket.
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
        }

        //Init the socket with a URI and register for events we want to know about.
        public void Init(string Uri)
        {
            //Initialize the socket
            try
            {
                sock = new WebSocket4Net.WebSocket(Uri, "graphql-ws");
                sock.Opened += (sender, data) => { OnConnect(data); };
                sock.MessageReceived += (sender, data) => { OnMessage(data); };
                sock.Closed += (sender, data) => { OnDisconnect(data); };
                sock.DataReceived += (sender, data) => { OnData(data); };
            }
            catch (Exception ex)
            {
                //Init failed 
                sock = null;
                isConnected = false;
            }
        }

        private void OnData(DataReceivedEventArgs data)
        {
            System.Diagnostics.Debug.WriteLine("/n/n");
            System.Diagnostics.Debug.WriteLine(data.Data);
        }

        private async void OnMessage(MessageReceivedEventArgs data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("/n/n");
                System.Diagnostics.Debug.WriteLine(data.Message);
                if (data.Message.Contains("actionLogged"))
                {
                    var actionLoggedObject = JsonConvert.DeserializeObject<ActionLoggedRootObject>(data.Message);
                    var action = actionLoggedObject.payload.data.actionLogged.action;
                    bool hasJournal; 

                    if (action.entryDate != null)
                        hasJournal = true;
                    else
                        hasJournal = false;

                    //Need to figure out action type
                    await PlayerFeedAPI.UpdateEpisodeProperty(action.episodeId, action.listen, action.favorite, hasJournal, action.position);
                }
                //process incoming lastActions
                else if (data.Message.Contains("lastActions"))
                {
                    List<Edge> actionsList = new List<Edge>();  //list of actions
                    ActionsRootObject actionsObject = JsonConvert.DeserializeObject<ActionsRootObject>(data.Message);
                    if (actionsObject.payload.data.lastActions.pageInfo.hasNextPage == true)
                    {
                        foreach (Edge item in actionsObject.payload.data.lastActions.edges.OrderByDescending(x => x.createdAt))  //loop throgh them all and update episode data (without sending episode changed messages)
                        {
                            await PlayerFeedAPI.UpdateEpisodeProperty(item.episodeId, item.listen, item.favorite, item.hasJournal, item.position, false);
                        }
                        //since we told UpdateEpisodeProperty to NOT send a message to the UI, we need to do that now.
                        if (actionsObject.payload.data.lastActions.edges.Count > 0)
                        {
                            MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                        }
                        //Send last action query to the websocket
                        Variables variables = new Variables();
                        System.Diagnostics.Debug.WriteLine($"Getting actions since {GlobalResources.LastActionDate.ToString()}...");
                        var updateEpisodesQuery = "{ lastActions(date: \"" + GlobalResources.LastActionDate.ToString("o") + "Z\", cursor: \"" + actionsObject.payload.data.lastActions.pageInfo.endCursor + "\") { edges { id episodeId userId favorite listen position entryDate updatedAt createdAt } pageInfo { hasNextPage endCursor } } } ";
                        var updateEpisodesPayload = new WebSocketHelper.Payload(updateEpisodesQuery, variables);
                        var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", updateEpisodesPayload));
                        DabSyncService.Instance.Send(JsonIn);
                    }
                    else
                    {
                        if (actionsObject.payload.data.lastActions != null)
                        {
                            foreach (Edge item in actionsObject.payload.data.lastActions.edges.OrderByDescending(x => x.createdAt))  //loop throgh them all and update episode data (without sending episode changed messages)
                            {
                                await PlayerFeedAPI.UpdateEpisodeProperty(item.episodeId, item.listen, item.favorite, item.hasJournal, item.position, false);
                            }
                            //since we told UpdateEpisodeProperty to NOT send a message to the UI, we need to do that now.
                            if (actionsObject.payload.data.lastActions.edges.Count > 0)
                            {
                                MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged");
                            }
                        }

                        //store a new last action date
                        GlobalResources.LastActionDate = DateTime.Now.ToUniversalTime();
                    }                  
                }
                else if (data.Message.Contains("updatedEpisodes"))
                {
                    Resource resource = GlobalResources.Instance.resource;
                    LastEpisodeDateQueryHelper.LastEpisodeQueryRootObject episodesObject = JsonConvert.DeserializeObject<LastEpisodeDateQueryHelper.LastEpisodeQueryRootObject>(data.Message);

                    if (episodesObject.payload.data.updatedEpisodes.pageInfo.hasNextPage == true)
                    {
                        foreach (var item in episodesObject.payload.data.updatedEpisodes.edges)
                        {
                            allEpisodes.Add(item);
                        }
                        //PlayerFeedAPI.GetEpisodes(resource, episodesObject);
                        //send websocket message to get episodes by channel
                        string lastEpisodeQueryDate = GlobalResources.GetLastEpisodeQueryDate(resource.id);
                        Variables variables = new Variables();
                        Debug.WriteLine($"Getting episodes by ChannelId");
                        var episodesByChannelQuery = "query { updatedEpisodes(date: \"" + lastEpisodeQueryDate + "\", channelId: " + resource.id + ", cursor: \"" + episodesObject.payload.data.updatedEpisodes.pageInfo.endCursor + "\") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                        var episodesByChannelPayload = new WebSocketHelper.Payload(episodesByChannelQuery, variables);
                        var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", episodesByChannelPayload));
                        DabSyncService.Instance.Send(JsonIn);
                        //loop through and do stuff
                    }
                    else
                    {
                        foreach (var item in episodesObject.payload.data.updatedEpisodes.edges)
                        {
                            allEpisodes.Add(item);
                        }
                        if (episodesObject.payload.data.updatedEpisodes != null)
                        {
                            await PlayerFeedAPI.GetEpisodes(resource, allEpisodes);
                            //do something
                        }
                    }
                    //store a new episode query date
                    GlobalResources.SetLastEpisodeQueryDate(resource.id);
                }
                else if (data.Message.Contains("actions")) //Should no longer be needed since we store user episode meta
                {
                    //process incoming new episode data
                    List<Edge> actionsList = new List<Edge>();  //list of actions
                    ActionsRootObject actionsObject = JsonConvert.DeserializeObject<ActionsRootObject>(data.Message);
                    if (actionsObject.payload.data.actions != null) //make sure we got somethign back
                    {
                        System.Diagnostics.Debug.WriteLine($"Received {actionsObject.payload.data.actions.edges.Count} actions...");
                        foreach (Edge item in actionsObject.payload.data.actions.edges.OrderByDescending(x => x.createdAt))//loop throgh them all in most recent order first and update episode data (without sending episode changed messages)
                        {
                            await PlayerFeedAPI.UpdateEpisodeProperty(item.episodeId, item.listen, item.favorite, item.hasJournal, item.position, false);

                        }
                        MessagingCenter.Send<string>("dabapp", "EpisodeDataChanged"); //tell listeners episodes have changed.
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in MessageReceived: " + ex.ToString());
            }
        }

        private object OnEvent(string s, object data)
        {
            //A requested event has fired - notify the calling app so it can handle it.

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler(s, data.ToString()));

            return data;
        }


        private object OnReconnect(object data)
        {
            //Socket has reconnected
            isConnected = true;

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("reconnected", data.ToString()));

            //Return
            return data;
        }

        private object OnConnect(object data)
        {
            //Socket has connected (1st time probably)
            isConnected = true;
            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("connected", data.ToString()));
            //Return

            return data;
        }

        private object OnDisconnect(object data)
        {
            //Socket has disconnected
            isConnected = false;

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("disconnected", data.ToString()));

            //Return
            return data;
        }

        public void Disconnect()
        {
            //Disconnect the socket
            if (IsConnected)
            {
                sock.Close();
            }
        }

        public void Send(string JsonIn)
        {
            sock.Send(JsonIn);
        }


        public void Connect()
        {
            //Connect the socket
            sock.Open();
        }
    }
}