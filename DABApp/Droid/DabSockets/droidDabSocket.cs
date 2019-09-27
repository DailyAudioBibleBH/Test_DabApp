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
using Quobject.SocketIoClientDotNet.Client;
using Xamarin.Forms;

[assembly: Dependency(typeof(DroidDabSocket))]
namespace DABApp.Droid.DabSockets
{
    public class DroidDabSocket : IDabSocket
    {
        bool isInitialized = false;
        bool isConnected = false;
        Socket sock;

        public event EventHandler<DabSocketEventHandler> DabSocketEvent;

        public DroidDabSocket()
        {
        }

        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
        }

        public void Connect(string token)
        {
            //Make sure the socket is initialized
            if (!isInitialized) throw new Exception("You must initialize the socket before using it");
            sock.Connect();

        }

        public void Init(string Uri, List<String> events)
        {
            //Initialize the socket
            try
            {
                sock = IO.Socket(Uri);
                isInitialized = true;

                //Set up standard events
                sock.On("connect", data => OnConnect(data));
                sock.On("disconnect", data => OnDisconnect(data));
                sock.On("reconnect", data => OnReconnect(data));
                sock.On("reconnecting", data => OnReconnecting(data));


                //Set up custom events requested by the caller
                foreach (string s in events)
                {
                    sock.On(s, data => OnEvent(s, data));
                }


            }
            catch (Exception ex)
            {
                isInitialized = false;
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

        private object OnReconnecting(object data)
        {
            //Socket is reconnecting

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("reconnecting", data.ToString()));

            //Return
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
            //Socket has connected (1st time)
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
            if (IsConnected)
            {
                sock.Disconnect();
            }
        }

        public void Connect()
        {
            sock.Connect();
        }

        public void Emit(string Command, object Data)
        {
            sock.Emit(Command, Data);
        }
    }
}