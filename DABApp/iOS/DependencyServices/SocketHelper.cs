using System;
namespace DABApp.iOS
{
	public class SocketHelper
	{
		public object date { get; set;}
		public object token { get; set;}

		public SocketHelper(string Date, string Token)
		{
			date = (object)Date;
			token = (object)Token;
		}
	}
}
