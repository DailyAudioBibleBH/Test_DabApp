using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DABApp;

namespace DABApp
{
    //General interface for socket connections
	public interface IDabSocket
    {
        void Init(string Uri, List<String> events); //Init the socket connection
		void Connect(); //Connect
        void Disconnect(); //Disconnect
        void Emit(string Command, object Data); //Send generic data to the socket
        bool IsConnected { get; }
        event EventHandler<DabSocketEventHandler> DabSocketEvent;
    }
}
