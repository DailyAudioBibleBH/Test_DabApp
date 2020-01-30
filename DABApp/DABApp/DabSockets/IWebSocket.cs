using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabSockets
{
    public interface IWebSocket
    {
        void Init(string Uri); //Init the socket connection
        void Connect(); //Connect
        void Send(string JsonIn);
        void Disconnect(); //Disconnect
        bool IsConnected { get; }
        event EventHandler<DabSocketEventHandler> DabSocketEvent; //raised when generic websocket events happen
        event EventHandler<DabGraphQlMessageEventHandler> DabGraphQlMessage; //raised with graph ql sends a message
    }
}

