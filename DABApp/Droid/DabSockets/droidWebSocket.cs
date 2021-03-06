using System;
using DABApp.Droid.DabSockets;
using Xamarin.Forms;
using DABApp.DabSockets;
using WebSocket4Net;
using System.Diagnostics;
using DataReceivedEventArgs = WebSocket4Net.DataReceivedEventArgs;
using DABApp.Service;

[assembly: Dependency(typeof(droidWebSocket))]
namespace DABApp.Droid.DabSockets
{
    public class droidWebSocket : IWebSocket
    {
        bool isConnected = false;
        WebSocket4Net.WebSocket sock;
        public event EventHandler<DabSocketEventHandler> DabSocketEvent;
        public event EventHandler<DabGraphQlMessageEventHandler> DabGraphQlMessage;

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
            DabServiceEvents.TrafficOccured(Service.GraphQlTrafficDirection.Inbound, data.Message);
            Debug.WriteLine("SOCKET RCVD:" + data.Message);
            DabGraphQlMessage?.Invoke(this, new DabGraphQlMessageEventHandler(data.Message));
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
            JsonIn = GlobalResources.CleanupJson(JsonIn); //prep the json

            DabServiceEvents.TrafficOccured(Service.GraphQlTrafficDirection.Outbound, JsonIn);
            Debug.WriteLine("SOCKET SEND:" + JsonIn);
            sock.Send(JsonIn);
        }


        public void Connect()
        {
            //Connect the socket
            sock.Open();
        }
    }
}