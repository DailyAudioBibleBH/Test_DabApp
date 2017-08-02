using System;
namespace DABApp
{
	public interface ISocket
	{
		void Connect(string token); //connect ot the server
		void Join(string date); //join a room
		void Key(string html, string date); //send content to the socket
		string content { get;} //text content form the socket
		bool IsConnected { get;}
		event EventHandler OnUpdate;
        event EventHandler OnConnect;
        event EventHandler OnDisconnect;
	}
}
