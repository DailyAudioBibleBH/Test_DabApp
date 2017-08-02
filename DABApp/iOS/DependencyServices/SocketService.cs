using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class SocketService : ISocket
    {
        Socket socket;
        static bool connected = false;
        static bool joined = false;
        static bool NotifyDis = true;
        static bool NotifyRe = true;
        static bool externalUpdate = true;
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
            //Connect to the server
            socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
            socket.Connect();
            connected = true;
            Token = token;

            socket.On("connect", data =>
             {
                 Debug.WriteLine("connect:" + JsonConvert.SerializeObject((data)));
                 connected = true;
             });

            socket.On("connect_error", error =>
			 {
			     Debug.WriteLine("connect_error:" + JsonConvert.SerializeObject((error)));
			 });

            socket.On("connect_timeout", data =>
			{
			    Debug.WriteLine("connect_timeout:" + JsonConvert.SerializeObject((data)));
			});

            socket.On("disconnect", data =>
            {
                Debug.WriteLine("disconnect:" + JsonConvert.SerializeObject((data)));
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
                Debug.WriteLine("reconnect:" + JsonConvert.SerializeObject((data)));
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
                else
                {
                    if (!string.IsNullOrEmpty(_date))
                    {
                        Join(_date);
                    }
                }
            });
            socket.On("reconnect_attempt", data =>
            {
                Debug.WriteLine("reconnect_attempt:" + JsonConvert.SerializeObject((data)));
            });

            socket.On("reconnecting", data =>
            {
                Debug.WriteLine("reconnecting:" + JsonConvert.SerializeObject((data)));
                //Reconnecting(data, new EventArgs());
            });

            socket.On("reconnect_error", data =>
            {
                Debug.WriteLine("reconnect_attempt:" + JsonConvert.SerializeObject((data)));
            });

            socket.On("reconnect_failed", data =>
			{
			    Debug.WriteLine("reconnect_failed:" + JsonConvert.SerializeObject((data)));
			});


            socket.On("room_error", data =>
            {
                Debug.WriteLine("room_error:" + JsonConvert.SerializeObject((data)));
                joined = false;
                Room_Error(data, new EventArgs());
            });
            socket.On("auth_error", data =>
            {
                Debug.WriteLine("auth_error:" + JsonConvert.SerializeObject((data)));
                Auth_Error(data, new EventArgs());
            });
            socket.On("join_error", data =>
            {
                Debug.WriteLine("join_error:" + JsonConvert.SerializeObject((data)));
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
                Debug.WriteLine("join:" + JsonConvert.SerializeObject((Data)));
                socket.Emit("join", Data);
                joined = true;
                socket.On("update", data =>
                {
                    Debug.WriteLine("update:" + JsonConvert.SerializeObject((data)));
                    if (externalUpdate)
                    {
                        var jObject = data as JToken;
                        var Date = jObject.Value<string>("date");
                        if (Date == _date)
                        {
                            content = converter.Convert(jObject.Value<string>("content"));
                            contentChanged(this, new EventArgs());
                        }
                    }
                    else externalUpdate = true;
                });
            }
        }

        public void Key(string html, string date)
        {
            if (connected)
            {
                var help = new SocketHelper(md.Transform(html), date, Token);
                var Data = JObject.FromObject(help);
                //Emit a Key event with the HTML to be sent
                //socket.Emit("join", Data);
                //Debug.WriteLine("join:" + JsonConvert.SerializeObject((Data)));
                socket.Emit("key", Data);
                Debug.WriteLine("key:" + JsonConvert.SerializeObject((Data)));
                externalUpdate = false;
            }
            else
            {
                StoredHtml = html;
            }
        }

        public string content { get; set; }

        public bool IsConnected
        {
            get { return connected; }
        }
    }
}
