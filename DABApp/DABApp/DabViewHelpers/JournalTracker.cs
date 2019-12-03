//using System;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using Acr.DeviceInfo;
//using Plugin.Connectivity;
//using SQLite;
//using Xamarin.Forms;

//namespace DABApp
//{
//    public class JournalTracker : INotifyPropertyChanged
//    {
//        public ISocket socket;
//        private string _Content;
//        private bool _IsConnected = false;
//        private bool _IsJoined = false;
//        private double _EntryHeight = 400;
//        private bool Once = false;

//        public static JournalTracker Current { get; private set; }

//        static JournalTracker()//Static constructor which changes the EntryHeight depending on if the device is iOS or Android.
//        {
//            Current = new JournalTracker();
//            if (Device.RuntimePlatform == Device.iOS)
//            {
//                Current.EntryHeight = DeviceInfo.Hardware.ScreenHeight * .8;
//            }
//            else
//            {
//                var modified = DeviceInfo.Hardware.ScreenHeight / GlobalResources.Instance.AndroidDensity;
//                Current.EntryHeight = modified * .6;//Changing the screen height of the journal based on the ScreenHeight divided by the density.
//            }
//        }

//        private JournalTracker()//Private constructor which connects the socket and joins the user to the room.
//        {
//            socket = DependencyService.Get<ISocket>();//Getting socket dependency service
//            _Content = socket.content;
//            socket.contentChanged += OnContentChanged;//Setting contentChanged event. To update when the socket updates.
//            socket.OnForcefulLogout += Socket_OnForcefulLogout;
//            Device.StartTimer(TimeSpan.FromMilliseconds(100), () => //Checks the socket connection to the server every 1/10th of a second
//            {
//                if (_IsConnected != socket.IsConnected)//If socket is not connected try to reconnect.
//                {
//                    IsConnected = socket.IsConnected;
//                    if (_IsConnected)
//                    {
//                        EntryHeight += 20;
//                    }
//                    else
//                    {
//                        if (Once)
//                        {
//                            EntryHeight -= 20;
//                        }
//                        else Once = true;
//                    }
//                }
//                if (_IsJoined != socket.IsJoined)//If room is not joined try to rejoin the room
//                {
//                    IsJoined = socket.IsJoined;
//                }
//                return true;
//            });
//        }

//        private void Socket_OnForcefulLogout(object sender, EventArgs e)
//        {
//            //Expire the token (should log the user out?)
//            SQLiteConnection db = DabData.database;
//            var expiration = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "TokenExpiration");
//            expiration.Value = DateTime.Now.AddSeconds(-1).ToString();
//            db.Update(expiration);

//        }

//        public string Content
//        {
//            get
//            {
//                return _Content;
//            }
//            set
//            {
//                _Content = value;
//                OnPropertyChanged("Content");
//            }
//        }

//        public bool IsConnected
//        {
//            get
//            {
//                return _IsConnected;
//            }
//            set
//            {
//                _IsConnected = value;
//                OnPropertyChanged("IsConnected");
//            }
//        }

//        public bool IsJoined
//        {
//            get
//            {
//                return _IsJoined;
//            }
//            set
//            {
//                _IsJoined = value;
//                OnPropertyChanged("IsJoined");
//            }
//        }

//        public double EntryHeight
//        {
//            get
//            {
//                return _EntryHeight;
//            }
//            set
//            {
//                _EntryHeight = value;
//                OnPropertyChanged("EntryHeight");
//            }
//        }

//        void OnContentChanged(object o, EventArgs e)//Updates the content displayed on the journal page so that it matches what's on the web socket server.
//        {
//            Content = socket.content;
//        }

//        public void Connect(string token)//Connecting to the web socket server.
//        {
//            if (Open)//Prevents the app from reconnecting when it is dismissed in the background
//            {
//                socket.Connect(token);
//            }
//        }

//        public void Update(string date, string html)//Called when changes to the text is made by the user.
//        {
//            socket.Key(html, date);
//        }

//        public void Join(string date)
//        {//Called in order to join the room
//            socket.Join(date);
//        }

//        public bool Open { get; set; } = true;

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
//        {
//            var handler = PropertyChanged;
//            if (handler != null)
//                handler(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }
//}
