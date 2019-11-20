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

            //Init the socket
            sock.Init(uri);

            return true;
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
            Payload token = new Payload(Token.Value);
            var ConnectInit = JsonConvert.SerializeObject(new ConnectionInitSyncSocket("connection_init", token));
            sock.Send(ConnectInit);

            var variables = new Variables();
            var query = "subscription {\n actionLogged {\n action {\n userId\n episodeId\n listen\n position\n favorite\n entryDate\n }\n }\n }";
            WebSocketHelper.Payload payload = new WebSocketHelper.Payload(query, variables);
            var SubscriptionInit = JsonConvert.SerializeObject(new WebSocketSubscription("start", payload));
            sock.Send(SubscriptionInit);

            //Grab existing episode data
            //Error about unopened database
            //var updateEpisodesQuery = "query{ lastActions(date: " + GlobalResources.GetLastActionDate + ") { edges { id episodeId userId favorite listen position entryDate updatedAt createdAt } } } ";
            //var updateEpisodesPayload = new WebSocketHelper.Payload(updateEpisodesQuery, variables);
            //var JsonIn = JsonConvert.SerializeObject(new WebSocketCommunication("start", updateEpisodesPayload));
            //DabSyncService.Instance.Send(JsonIn);
            //GlobalResources.GetLastActionDate = DateTime.UtcNow.ToString();
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
