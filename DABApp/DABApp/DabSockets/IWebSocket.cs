using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabSockets
{
    public interface IWebSocket
    {
        void Init(string Uri, List<String> events); //Init the socket connection
        void Connect(); //Connect
        void Send();
        void Disconnect(); //Disconnect
        void Emit(string Command, object Data); //Send generic data to the socket
        bool IsConnected { get; }
        event EventHandler<DabSocketEventHandler> DabSocketEvent;
    }
}

