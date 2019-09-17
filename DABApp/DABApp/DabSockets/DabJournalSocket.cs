using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Forms;

namespace DABApp.DabSockets
{
    public class DabJournalSocket : INotifyPropertyChanged
    {
        IDabSocket sock;
        string currentContent;
        DateTime currentDate;

        public event PropertyChangedEventHandler PropertyChanged;

        //Create a journalling socket basec on an instance of a generic socket
        public DabJournalSocket()
        {
            //INIT THE SOCKET

        }

        public bool InitAndConnect()
        {

            //Create a socket
            sock = DependencyService.Get<IDabSocket>(DependencyFetchTarget.NewInstance);

            //Get the URL to use
            ContentConfig config = ContentConfig.Instance;
            string uri;
            if (GlobalResources.TestMode)
            {
                uri = config.app_settings.stage_journal_link;
            }
            else
            {
                uri = config.app_settings.prod_journal_link;
            }

            //Create list of events to monitor (basic connection events are already monitored)
            List<String> events = new List<String>();
            events.Add("room_error");
            events.Add("join_error");
            events.Add("auth_error");
            events.Add("update");

            //Init the socket
            sock.Init(uri, events);

            //Register for notifications from the socket
            sock.DabSocketEvent += Sock_DabSocketEvent;

            //Connect the socket
            sock.Connect();

            return true;

        }

        public bool UpdateJournal(DateTime date, string content)
        {
            //Sends new content data to the journal socket 
            var room = date.ToString("yyyy-MM-dd");
            var token = AuthenticationAPI.CurrentToken;
            var data = new DabJournalObject(content, room, token);
            var json = JObject.FromObject(data);
            //Send data to the socket
            sock.Emit("key", json);

            return true;

        }

        public bool JoinRoom(DateTime date)
        {
            //Joins a room for a specific date
            var room = date.ToString("yyyy-MM-dd");
            var token = AuthenticationAPI.CurrentToken;
            var data = new DabJournalObject(room, token);
            var json = JObject.FromObject(data);
            //Send data to the socket
            sock.Emit("join", json);
            //Store the date we're using
            currentDate = date;

            return true;

        }

        //IsConnected returns a bool indicating whether the socket is currently connected.
        //This is a bindable property
        public bool IsConnected
        {
            get
            {
                return sock.IsConnected;
            }
        }

        public string Content
        {
            get
            {
                return currentContent;
            }
            set
            {
                currentContent = value;
                UpdateJournal(currentDate, value);
                OnPropertyChanged("Content");

            }
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
                    break;
                case "connected": //Socket connected
                    Sock_Connected(e.data);
                    break;
                case "reconnecting": //Socket reconnecting
                    //Do nothing for now
                    break;
                case "reconnected": //Socket reconnected
                    Sock_Connected(e.data);
                    break;
                case "room_error": //Error with a room
                    Sock_ErrorOccured(e.eventName, e.data);
                    break;
                case "join_error": //Error joining
                    Sock_ErrorOccured(e.eventName, e.data);
                    break;
                case "auth_error": //Error with authentication
                    Sock_ErrorOccured(e.eventName, e.data);
                    break;
                case "update": //update happened externally
                    DabJournalObject data = JsonConvert.DeserializeObject<DabJournalObject>(e.data);
                    currentContent = e.data.ToString();
                    OnPropertyChanged("Content");
                    break;
                default:
                    break;
            }
        }

        private void Sock_ErrorOccured(string eventName, object data)
        {
            //The socket has encountenered an error. Take appropriate action.

            //For now, disconnect and don't reconnect
            if (sock.IsConnected)
            {
                sock.Disconnect();
            }

            OnPropertyChanged("IsConnected");
        }

        private void Sock_Connected(object data)
        {
            //The socket has connected or reconnected. Take appropriate action

            //Notify UI
            OnPropertyChanged("IsConnected");
        }

        /* Events to handle Binding */
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
