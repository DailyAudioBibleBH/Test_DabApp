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
        bool IsConnected { get; }
        event EventHandler<DabSocketEventHandler> DabSocketEvent;


        //void Join(string date);
        //void Key(string html, string date);
        //string content { get;}
        //bool ExternalUpdate { get; set;}
        //bool IsJoined { get; }
        //      event EventHandler contentChanged;
        //event EventHandler Disconnect;
        //event EventHandler Reconnect;
        //event EventHandler Reconnecting;
        //event EventHandler Room_Error;
        //event EventHandler Auth_Error;
        //event EventHandler Join_Error;
        //      event EventHandler OnForcefulLogout;
    }
}
