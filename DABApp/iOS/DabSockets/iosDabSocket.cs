using System;
using System.Collections.Generic;
using DABApp;
using DABApp.iOS.DabSockets;
using Quobject.SocketIoClientDotNet.Client;
using Xamarin.Forms;

[assembly: Dependency(typeof(IosDabSocket))]
namespace DABApp.iOS.DabSockets
{

    //This is the generic iOS implementation of a socket connection

    public class IosDabSocket: IDabSocket
    {

        bool isInitialized = false;
        bool isConnected = false;
        Socket sock;

        public event EventHandler<DabSocketEventHandler> DabSocketEvent;

        public IosDabSocket()
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
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler(s, data));

            return data;
        }

        private object OnReconnecting(object data)
        {
            //Socket is reconnecting

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("reconnecting", data));

            //Return
            return data;
        }

        private object OnReconnect(object data)
        {
            //Socket has reconnected
            isConnected = true;

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("reconnected", data));

            //Return
            return data;
        }

        private object OnDisconnect(object data)
        {
            //Socket has disconnected
            isConnected = false;

            //Notify the listener
            DabSocketEvent?.Invoke(this, new DabSocketEventHandler("disconnected", data));

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
    }
}
