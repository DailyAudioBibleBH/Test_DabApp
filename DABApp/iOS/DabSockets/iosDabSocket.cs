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

        bool isConnected = false;
        Socket sock;

        public event EventHandler<DabSocketEventHandler> DabSocketEvent;

        public IosDabSocket()
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
        public void Init(string Uri, List<String> events)
        {
            //Initialize the socket
            try
            {
                sock = IO.Socket(Uri);

                //Set up standard events
                sock.On("connect",data => OnConnect(data));
                sock.On("disconnect", data => OnDisconnect(data));
                sock.On("reconnect", data => OnReconnect(data));
                sock.On("reconnecting", data => OnEvent("reconnecting",data)); //Use basic OnEvent since nothing is "done" yet


                //Set up custom events requested by the caller
                foreach (string s in events)
                {
                    sock.On(s, data => OnEvent(s, data));
                }


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
            isConnected = true;

            //Update UI
            MessagingCenter.Send<string>("dabapp", "SocketConnected");

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

            //Update the UI
            MessagingCenter.Send<string>("dabapp", "SocketDisconnected");

            //Return
            return data;
        }

        public void Disconnect()
        {
            //Disconnect the socket
            if (IsConnected)
            {
                sock.Disconnect();
            }
        }

        public void Connect()
        {
            //Connect the socket
            sock.Connect();
        }

        public void Emit(string Command, object Data)
        {
            //Send data to the socket
            sock.Emit(Command, Data);
        }


    }
}
