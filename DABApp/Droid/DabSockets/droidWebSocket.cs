﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.Droid.DabSockets;
using Xamarin.Forms;
using DABApp.DabSockets;

[assembly: Dependency(typeof(droidWebSocket))]
namespace DABApp.Droid.DabSockets
{
    public class droidWebSocket : IWebSocket
    {

        bool isConnected = false;
        WebSocket sock;
        public event EventHandler<DabSocketEventHandler> DabSocketEvent;

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
                sock = new WebSocket(Uri, "graphql-ws");
                sock.OnOpen += (sender, data) => { OnConnect(data); };
                sock.OnMessage += (sender, data) => { };
                sock.OnClose += (sender, data) => { OnDisconnect(data); };
            }
            catch (Exception ex)
            {
                //Init failed 
                sock = null;
                isConnected = false;
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

        public void Send()
        {
            sock.Send("test");
        }


        public void Connect()
        {
            //Connect the socket
            sock.ConnectAsync();
        }
    }
}