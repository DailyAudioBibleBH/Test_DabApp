using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        IDabSocket sock; //The socket connection
        public event PropertyChangedEventHandler PropertyChanged;

        private DabSyncService()
        {
            //Constructure is private so we only allow one of them
        }

        public bool Init()
        {
            //Set up the socket and connect it so it can be used throughout the app.

            //Create socket
            sock = DependencyService.Get<IDabSocket>(DependencyFetchTarget.NewInstance);

            //Get the URL to use
            var appSettings = ContentConfig.Instance.app_settings;
            string uri = (GlobalResources.TestMode) ? appSettings.stage_service_link : appSettings.prod_service_link;
            //need to add wss:// since it just gives us the address here
            uri = $"wss://{uri}";

            //Create a list of events to monitor
            List<String> events = new List<String>();
            //events.Add("");

            //Register for notifications from the socket
            sock.DabSocketEvent += Sock_DabSocketEvent;

            //Init the socket
            sock.Init(uri, events);

            //Connect the socket
            sock.Connect();

            return true;

        }

        private void Sock_DabSocketEvent(object sender, DabSocketEventHandler e)
        {
            //An event has been fired by the socket. Respond accordingly

            //Log the event to the debugger
            Debug.WriteLine($"{e.eventName} was fired with {e.data}");

            //Take action on the event
            switch (e.eventName.ToLower())
            {
                default:
                    break;
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
