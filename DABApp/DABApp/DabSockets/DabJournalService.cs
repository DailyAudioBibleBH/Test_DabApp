using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
//using Android.Content;
//using Android.Views.InputMethods;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Forms;
using Html2Markdown;
using SQLite;

/* This is the DAB Journal Service that drives the journalling system of the DAB App.
 * It uses native iDabSocket objects to maintain a connection to the socket
 * This socket registers for several custom events such as "update" so it knows when
 * the journal has been updated externally.
 */

namespace DABApp.DabSockets
{
    public class DabJournalService : INotifyPropertyChanged
    {
        IDabSocket sock; //The socket connection
        string currentContent; //Current content of the journal
        DateTime currentDate; //Current date of the journal
        public event PropertyChangedEventHandler PropertyChanged; //For binding
        public string content { get; set; } //Bindable content property (internal use)
        public bool ExternalUpdate = true; //Helps us only update content when externally updated
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;

        //Create a journalling socket basec on an instance of a generic socket
        public DabJournalService()
        {
            //Nothing to do here
        }

        public void Reconnect()
        {
            //Reconnect the socket if needed
            if (sock == null)
            {
                InitAndConnect();
            }
            else
            {
                sock.Connect();
            }
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

            //Register for notifications from the socket
            sock.DabSocketEvent += Sock_DabSocketEvent;

            //Init the socket
            sock.Init(uri, events);

            //Connect the socket
            sock.Connect();

            return true;
        }

        public bool UpdateJournal(DateTime date, string content)
        {
            //Sends new content data to the journal socket 
            var room = date.ToString("yyyy-MM-dd");
            var token = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Token;
            var data = new DabJournalObject(content, room, token);
            data.html = CommonMark.CommonMarkConverter.Convert(content);
            var json = JObject.FromObject(data);
            //Send data to the socket 
            if (!ExternalUpdate)
            {
                sock.Emit("key", json);
            }
            return true;
        }

        public bool JoinRoom(DateTime date)
        {
            //Joins a room for a specific date
            var room = date.ToString("yyyy-MM-dd");
            var token = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Token;
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

        //Bindable content - send it off to the server if it's being changed.
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
                    Sock_ExternalUpdateOccured(e.eventName, e.data);
                    break;
                default:
                    break;
            }
        }

        public void Sock_ExternalUpdateOccured(string eventName, string json)
        {
            //The journal was updated externally.
            DabJournalObject data = JsonConvert.DeserializeObject<DabJournalObject>(json);
            if (ExternalUpdate)
            {
                string html = data.content;

                //Convert the content from HTML to MarkDown
                //get rid of line breaks in the HTML
                html = html.Replace("\n", "");
                content = new Converter().Convert(html);
                //Replace extra \n\n with \n
                content = content.Replace("\n\n", "\n");
                //trim off a leading \n
                if (content.StartsWith("\n",StringComparison.CurrentCulture))
                {
                    content = content.Substring(1);
                }

                currentContent = content;
            }

            //Notify UI that things have changed (assume we are now connected)
            OnPropertyChanged("Content");
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsDisconnected");
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
