using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.iOS;
using Html2Markdown;
using MarkdownDeep;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using Xamarin.Forms;

[assembly: Dependency(typeof(SocketService))]
namespace DABApp.iOS
{
	public class SocketService: ISocket
	{
		static Socket socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
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
		static Markdown md;
		static Converter converter;

		static SocketService()
		{
			md = new MarkdownDeep.Markdown();
			md.SafeMode = false;
			md.ExtraMode = true;
			md.MarkdownInHtml = true;
			converter = new Converter();
		}

		public void Connect(string token)
		{
			socket.Connect();
			connected = true;
			Token = token;
			socket.On("disconnect", data => 
			{
				Debug.Write($"Disconnect {data} {DateTime.Now}");
				connected = false;
				if (NotifyDis)
				{
					Disconnect(data, new EventArgs());
					NotifyDis = false;
					NotifyRe = true;
				}
				socket.Connect();
			});
			socket.On("reconnect", data =>
			{
				Debug.Write($"Reconnected {data} {DateTime.Now}");
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
				else {
					if (!string.IsNullOrEmpty(_date))
					{
						Join(_date);
					}
				}
			});
			socket.On("reconnecting", data => 
			{
				Debug.Write($"Reconnecting {data} {DateTime.Now}");
				//Reconnecting(data, new EventArgs());
			});
			socket.On("room_error", data => 
			{
				Debug.Write($"Room_error {data} {DateTime.Now}");
				joined = false;
				Room_Error(data, new EventArgs());
			});
			socket.On("auth_error", data => 
			{
				Debug.Write($"Auth_error {data} {DateTime.Now}");
				Auth_Error(data, new EventArgs());
			});
			socket.On("join_error", data => 
			{
				Debug.Write($"Join_error {data} {DateTime.Now}");
				joined = false;
				Join_Error(data, new EventArgs());
			});
			Debug.Write($"Connected {DateTime.Now}");
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
					Debug.Write($"Update {data} {DateTime.Now}");
 					if (ExternalUpdate)
					{
						var jObject = data as JToken;
						var Date = jObject.Value<string>("date");
						if (Date == _date)
						{
							content = converter.Convert(jObject.Value<string>("content"));
							contentChanged(this, new EventArgs());
						}
					}
				});
				Debug.Write("Join");
			}
		}

		public void Key(string html, string date) 
		{
			if (connected)
			{
				var help = new SocketHelper(md.Transform(html), date, Token);
				var Data = JObject.FromObject(help);
				if (!joined)
				{
					socket.Emit("join", Data);
				}
				socket.Emit("key", Data);
			}
			else {
				StoredHtml = html;
			}
			Debug.Write("Key");
		}

		public string content { get; set;}

		public bool IsConnected 
		{
			get { return connected; }
		}

		public bool IsJoined
		{ 
			get { return joined;}
		}

		public bool ExternalUpdate { get; set; } = true;
	}
}
