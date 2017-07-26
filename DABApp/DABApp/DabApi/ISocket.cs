using System;
namespace DABApp
{
	public interface ISocket
	{
		void Connect(string token);
		void Join(string date);
		void Key(string html, string date);
		string content { get;}
		event EventHandler contentChanged;
	}
}
