using System;
using System.Collections.Immutable;
using System.Diagnostics;
using DABApp.Droid;
using Html2Markdown;
using MarkdownDeep;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using Xamarin.Forms;

[assembly: Dependency(typeof(SocketService))]
namespace DABApp.Droid
{
	public class SocketService : ISocket
	{
		static Socket socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
		static bool connected = false;
		static bool joined = false;
		static bool NotifyDis = true;
		static bool NotifyRe = true;
		static string Token;
		static string _date;
		static string StoredHtml = null;
		static Markdown md;
		static Converter converter;

		public SocketService()
		{
			md = new MarkdownDeep.Markdown();
			md.SafeMode = false;
			md.ExtraMode = true;
			md.MarkdownInHtml = true;
			converter = new Converter();
		}

		public string content { get; set; }

		public bool ExternalUpdate { get; set; } = true;

		public bool IsConnected
		{
			get
			{
				return connected;
			}
		}

		public bool IsJoined
		{
			get
			{
				return joined;
			}
		}

		public event EventHandler Auth_Error;
		public event EventHandler contentChanged;
		public event EventHandler Disconnect;
		public event EventHandler Join_Error;
		public event EventHandler Reconnect;
		public event EventHandler Reconnecting;
		public event EventHandler Room_Error;

		public void Connect(string token)
		{
			try
			{
				if (socket == null)
				{ 
					socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
				}
				socket.Connect();
				connected = true;
				Token = token;
				socket.On("disconnect", data =>
				{
					Debug.WriteLine($"Disconnect {data} {DateTime.Now}");
					connected = false;
					if (NotifyDis && Disconnect != null)
					{
						Disconnect(data, new EventArgs());
						NotifyDis = false;
						NotifyRe = true;
					}
					try
					{
						if(socket == null)
						{
							socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
						}
						socket.Connect();
					}
					catch(Exception ex)
					{
						Debug.WriteLine($"Exception caught in Droid SocketService.Connect(): {ex.Message}");
					}
				});
				socket.On("reconnect", data =>
				{
					Debug.WriteLine($"Reconnected {data} {DateTime.Now}");
					connected = true;
					if (NotifyRe && Reconnect != null)
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
					else
					{
						if (!string.IsNullOrEmpty(_date))
						{
							Join(_date);
						}
					}
				});
				socket.On("reconnecting", data =>
				{
					Debug.WriteLine($"Reconnecting {data} {DateTime.Now}");
					//Reconnecting(data, new EventArgs());
				});
				socket.On("room_error", data =>
				{
					Debug.WriteLine($"Room_error {data} {DateTime.Now}");
					joined = false;
                    Room_Error?.Invoke(data, new EventArgs());
                });
				socket.On("auth_error", data =>
				{
					Debug.WriteLine($"Auth_error {data} {DateTime.Now}");
                    Auth_Error?.Invoke(data, new EventArgs());
                });
				socket.On("join_error", data =>
				{
					Debug.WriteLine($"Join_error {data} {DateTime.Now}");
					joined = false;
					Join_Error?.Invoke(data, new EventArgs());
				});
				socket.On(Socket.EVENT_CONNECT_ERROR, data=> {
					Debug.WriteLine($"SOCKET CONNECTION ERROR: {data.ToString()}");
            	});
				Debug.WriteLine($"Connected {DateTime.Now}");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Exception caught in Droid SocketService.Connect(): {e.Message}");
			}
		}

		public void Join(string date)
		{
			if (connected)
			{
				Debug.WriteLine($"Join: {date}");
				_date = date;
				var help = new SocketHelper(date, Token);
				var Data = JObject.FromObject(help);
				socket.Emit("join", Data);
				Debug.WriteLine($"join {Data}");
				joined = true;
				socket.Off("update");
				socket.On("update", data =>
				{
					Debug.WriteLine($"update {data}");
					if (ExternalUpdate)
					{
						var jObject = data as JToken;
						var Date = jObject.Value<string>("date");
						if (Date == _date)
						{
							string html = jObject.Value<string>("content");
							//get rid of line breaks in the HTML
							html = html.Replace("\n", "");
							content = converter.Convert(html);
							//Replace extra \n\n with \n
							content = content.Replace("\n\n", "\n");
							//trim off a leading \n
							if (content.StartsWith("\n"))
							{
								content = content.Substring(1);
							}
							if (content == null)
							{
								content = "";
							}
							Debug.WriteLine($"Join Content:{content} Join Html:{html}");
							contentChanged(this, new EventArgs());
						}
					}
				});
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
					Debug.WriteLine($"join {Data}");
				}
				socket.Emit("key", Data);
				Debug.WriteLine($"key {Data}");
			}
			else
			{
				StoredHtml = html;
			}
		}
	}
}
