using System;
namespace DABApp
{
	public interface ISocket
	{
		void Connect(string token);
		void Join(string date);
		void Key(string html, string date);
		string content { get;}
		bool IsConnected { get;}
		bool ExternalUpdate { get; set;}
		bool IsJoined { get;}
		event EventHandler contentChanged;
		event EventHandler Disconnect;
		event EventHandler Reconnect;
		event EventHandler Reconnecting;
		event EventHandler Room_Error;
		event EventHandler Auth_Error;
		event EventHandler Join_Error;
	}
}
