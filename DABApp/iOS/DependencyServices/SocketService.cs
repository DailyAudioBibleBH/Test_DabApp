﻿using System;
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
		static Socket socket = IO.Socket(GlobalResources.JournalUrl);
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
			try
			{
				if (socket == null)
				{ 
					socket = IO.Socket(GlobalResources.JournalUrl);
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
						if (socket == null)
						{ 
							socket = IO.Socket(GlobalResources.JournalUrl);
						}
						socket.Connect();
					}
					catch (Exception ex)
					{ 
						Debug.WriteLine($"Exception caught in iOS SocketService.Connect(): {ex.Message}");
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
                    if (Room_Error != null)
                    {
                        Room_Error(data, new EventArgs());
                    }
				});
				socket.On("auth_error", data =>
				{
					Debug.WriteLine($"Auth_error {data} {DateTime.Now}");
                    if (Auth_Error != null)
                    {
                        Auth_Error(data, new EventArgs());
                    }
				});
				socket.On("join_error", data =>
				{
					Debug.WriteLine($"Join_error {data} {DateTime.Now}");
					joined = false;
                    if (Join_Error != null)
                    {
                        Join_Error(data, new EventArgs());
                    }
				});
				socket.On(Socket.EVENT_CONNECT_ERROR, data=> {
					Debug.WriteLine($"SOCKET CONNECTION ERROR: {data.ToString()}");
            	});
				Debug.WriteLine($"Connected {DateTime.Now}");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Exception caught in iOS SocketService.Connect(): {e.Message}");
			}
		}

		public void Join(string date)
		{
			if (connected)
			{
				_date = date;
				var help = new SocketHelper(date, Token);
				var Data = JObject.FromObject(help);
				socket.Emit("join", Data);
				Debug.WriteLine($"join {Data}");
				joined = true;
                socket.Off("update");
				socket.On("update", data => {
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
							contentChanged?.Invoke(this, new EventArgs());
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
			else {
				StoredHtml = html;
			}
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
