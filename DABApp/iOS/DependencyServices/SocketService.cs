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
		static bool joined = false;
		static bool NotifyDis = true;
		static bool NotifyRe = true;
		static string Token;
		static string _date;
		static string StoredHtml = null;
		public event EventHandler contentChanged;
		public event EventHandler Disconnect;
		public event EventHandler Reconnect;
		public event EventHandler Reconnecting;
		public event EventHandler Room_Error;
		public event EventHandler Auth_Error;
		public event EventHandler Join_Error;

		public void Connect(string token)
		{
			socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
			socket.Connect();
			connected = true;
			Token = token;
			socket.On("disconnect", data => 
			{ 
				connected = false;
				if (NotifyDis)
				{
					Disconnect(data, new EventArgs());
					NotifyDis = false;
					NotifyRe = true;
				}
			});
			socket.On("reconnect", data =>
			{
				connected = true;
				if (NotifyRe)
				{
					Reconnect(data, new EventArgs());
					NotifyDis = true;
					NotifyRe = false;
				}
				if (StoredHtml != null)
				{
					joined = true;
					Key(StoredHtml, _date);
					StoredHtml = null;
				}
			});
			socket.On("reconnecting", data => 
			{
				//Reconnecting(data, new EventArgs());
			});
			socket.On("room_error", data => 
			{ 
				joined = false;
				Room_Error(data, new EventArgs());
			});
			socket.On("auth_error", data => 
			{
				Auth_Error(data, new EventArgs());
			});
			socket.On("join_error", data => 
			{ 
				joined = false;
				Join_Error(data, new EventArgs());
			});
		}

		public void Join(string date)
		{
			if (connected)
			{
				_date = date;
				var help = new SocketHelper(date, Token);
				var Data = JObject.FromObject(help);
				socket.Emit("join", Data);
				joined = true;
				socket.On("update", data => {
					var jObject = data as JToken;
					var Date = jObject.Value<string>("date");
					if (Date == _date)
					{
						content = jObject.Value<string>("content");
						contentChanged(this, new EventArgs());
					}
				});
			}
		}

		public void Key(string html, string date) 
		{
			if (connected && joined)
			{
				var help = new SocketHelper(html, date, Token);
				var Data = JObject.FromObject(help);
				socket.Emit("join", Data);
				socket.Emit("key", Data);
			}
			else {
				StoredHtml = html;
			}
		}

		public string content { get; set;}

		public bool IsConnected 
		{
			get { return connected; }
		}
	}
}
