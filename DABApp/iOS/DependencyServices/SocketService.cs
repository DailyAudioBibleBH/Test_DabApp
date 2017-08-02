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
        static bool _connected = false;
        static string _token;
        static string _date;
        public string _content { get; set; }
        public event EventHandler OnUpdate;
        public event EventHandler OnConnect;
        public event EventHandler OnDisconnect;

        static Markdown md;
        static Converter converter;

        static SocketService()
        {
            //Set up markdown / HTML converters
            md = new MarkdownDeep.Markdown();
            md.SafeMode = false;
            md.ExtraMode = true;
            md.MarkdownInHtml = true;
            converter = new Converter();
        }

        public SocketService()
        {
			socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
			socket.Connect();
        }

        public SocketService(string token)
        {
            //Connect to the server
            socket = IO.Socket("wss://journal.dailyaudiobible.com:5000");
            socket.Connect();
            

            _token = token;

            socket.On("connect", data =>
             {
                 Debug.WriteLine("connect:" + JsonConvert.SerializeObject((data)));
                 _connected = true;
                 OnConnect(this, new EventArgs());
             });

            socket.On("connect_error", error =>
             {
                 Debug.WriteLine("connect_error:" + JsonConvert.SerializeObject((error)));
                 _connected = false;
             });

            socket.On("connect_timeout", data =>
            {
                Debug.WriteLine("connect_timeout:" + JsonConvert.SerializeObject((data)));
                _connected = false;
            });

            socket.On("disconnect", data =>
            {
                Debug.WriteLine("disconnect:" + JsonConvert.SerializeObject((data)));
                _connected = false;
                OnDisconnect(this, new EventArgs());
            });

            socket.On("reconnect", data =>
            {
                Debug.WriteLine("reconnect:" + JsonConvert.SerializeObject((data)));
                _connected = true;
            });
            socket.On("reconnect_attempt", data =>
            {
                Debug.WriteLine("reconnect_attempt:" + JsonConvert.SerializeObject((data)));
            });

            socket.On("reconnecting", data =>
            {
                Debug.WriteLine("reconnecting:" + JsonConvert.SerializeObject((data)));
            });

            socket.On("reconnect_error", data =>
            {
                Debug.WriteLine("reconnect_attempt:" + JsonConvert.SerializeObject((data)));
                _connected = false;
            });

            socket.On("reconnect_failed", data =>
            {
                Debug.WriteLine("reconnect_failed:" + JsonConvert.SerializeObject((data)));
                _connected = false;
            });


            socket.On("room_error", data =>
            {
                Debug.WriteLine("room_error:" + JsonConvert.SerializeObject((data)));
            });

            socket.On("auth_error", data =>
            {
                Debug.WriteLine("auth_error:" + JsonConvert.SerializeObject((data)));
            });

            socket.On("join_error", data =>
            {
                Debug.WriteLine("join_error:" + JsonConvert.SerializeObject((data)));
            });
        }


        //Join a room for a given date
        public void Join(string date)
        {
            if (_connected)
            {
                _date = date;
                var help = new SocketHelper(date, _token);
                var Data = JObject.FromObject(help);

                Debug.WriteLine("join:" + JsonConvert.SerializeObject((Data)));
                socket.Emit("join", Data);

                socket.On("update", data =>
                {
                    Debug.WriteLine("update:" + JsonConvert.SerializeObject((data)));
                    var jObject = data as JToken;
                    var Date = jObject.Value<string>("date");
                    if (Date == _date)
                    {
                        _content = converter.Convert(jObject.Value<string>("content"));
                        OnUpdate(this, new EventArgs());
                    }
                });
            }
        }

        public void Key(string html, string date)
        {
            if (_connected)
            {
                var help = new SocketHelper(md.Transform(html), date, _token);
                var Data = JObject.FromObject(help);
                //Emit a Key event with the HTML to be sent
                //socket.Emit("join", Data);
                //Debug.WriteLine("join:" + JsonConvert.SerializeObject((Data)));
                socket.Emit("key", Data);
                Debug.WriteLine("key:" + JsonConvert.SerializeObject((Data)));
            }
            else
            {
                //Not connected
            }
        }

        public void Connect(string token)
        {
            throw new NotImplementedException();
        }

        public bool IsConnected
        {
            get { return _connected; }
        }

        public string content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
            }
        }
    }
}
