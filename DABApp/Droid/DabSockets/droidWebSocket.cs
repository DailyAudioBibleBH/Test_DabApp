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

[assembly: Dependency(typeof(droidWebSocket))]
namespace DABApp.Droid.DabSockets
{
    public class droidWebSocket : IWebSocket
    {

        bool isConnected = false;
        WebSocket sock;
        public event EventHandler<DabSocketEventHandler> DabSocketEvent;
        string actionType;
        bool listenedTo;


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
                var cookies = new List<KeyValuePair<string, string>>();
                var extension = new List<KeyValuePair<string, string>>();
                extension.Add(new KeyValuePair<string, string>("x-token", AuthenticationAPI.CurrentToken.ToString()));
                sock = new WebSocket(Uri, "graphql-ws");//, customHeaderItems:extension);
                sock.Opened += (sender, data) => { OnConnect(data); };
                sock.MessageReceived += (sender, data) => { OnMessage(data); };
                sock.Closed += (sender, data) => { OnDisconnect(data); };
            }
            catch (Exception ex)
            {
                //Init failed 
                sock = null;
                isConnected = false;
            }
        }

        private void OnMessage(MessageReceivedEventArgs data)
        {
            System.Diagnostics.Debug.WriteLine("/n/n");
            System.Diagnostics.Debug.WriteLine(data.Message);
            if (data.Message.Contains("actionLogged"))
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                var actionObject = JsonConvert.DeserializeObject<ActionLoggedRootObject>(data.Message, settings);
                var firstInstance = PlayerFeedAPI.GetEpisode(actionObject.payload.data.actionLogged.action.episodeId);
                FirstEpisodeCompare firstEpObject = new FirstEpisodeCompare(firstInstance.is_listened_to, (int)firstInstance.stop_time, firstInstance.is_favorite);
                var action = actionObject.payload.data.actionLogged.action;
                //if (firstEpObject.listen == "listened")
                //    listenedTo = true;
                //else
                //    listenedTo = false;

                if (firstEpObject.favorite != action.favorite)
                {
                    actionType = "favorite";
                }
                else if (listenedTo != action.listen)
                {
                    actionType = "listened";
                }
                else if (firstEpObject.position != action.position)
                {
                    actionType = "pause";
                }

                //Need to figure out action type
                AuthenticationAPI.CreateNewActionLog(action.episodeId, actionType, action.position, action.listen.ToString(), action.favorite);
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
            System.Diagnostics.Debug.WriteLine("Sync Socket Connected");
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