using System;
using System.Collections.Generic;
using DABApp.iOS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using Xamarin.Forms;

[assembly: Dependency(typeof(SocketService))]
namespace DABApp.iOS
{
	public class SocketService: ISocket
	{
		Socket socket;
		static bool connected = false;
		static string Token;
		public event EventHandler contentChanged;

		public void Connect(string token)
		{
			socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
			socket.Connect();
			connected = true;
			Token = token;
			socket.On("disconnect", data => { connected = false;});
			socket.On("reconnect", data => { connected = true;});
			socket.On("reconnecting", data => { });
			socket.On("room_error", data => { });
			socket.On("auth_error", data => { });
			socket.On("join_error", data => { });
		}

		public void Join(string date)
		{
			if (connected)
			{
				var help = new SocketHelper(date, Token);
				var Data = JObject.FromObject(help);
				socket.Emit("join", Data);
				socket.On("update", data => {
					var jObject = data as JToken;
					var Date = jObject.Value<string>("date");
					if (Date == date)
					{
						content = jObject.Value<string>("content");
						contentChanged(this, new EventArgs());
					}
				});
			}
		}

		public void Key(string html, string date) 
		{
			if (connected)
			{
				socket.Emit("key", html, date, Token);
			}
		}

		public string content { get; set;}
	}
}
