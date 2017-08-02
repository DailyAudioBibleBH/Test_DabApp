using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace DABApp
{
	public class DabJournalTracker : INotifyPropertyChanged
	{
		public ISocket socket;
		
        private string _Content;
		private bool _IsConnected = false; //Private connected tracker

		//public static JournalTracker Current { get; private set;}

		//static JournalTracker()
		//{
		//	Current = new JournalTracker();
		//}

		public DabJournalTracker()
		{
			socket = DependencyService.Get<ISocket>();
            //_Content = socket.content;
            socket.OnUpdate += OnReceiveContent;

			//Check for a connection property change
			Device.StartTimer(TimeSpan.FromMilliseconds(100), () => 
			{
				if (_IsConnected != socket.IsConnected)
				{
					IsConnected = socket.IsConnected;
				}
				return true;
			});
		}

		public string Content
		{ 
			get {
				return _Content;
			}
			set {
				_Content = value;
				OnPropertyChanged("Content");
			}
		}

		public bool IsConnected 
		{
			get {
				return _IsConnected;
			}
			set {
				_IsConnected = value;
				OnPropertyChanged("IsConnected");
			}
		}

		void OnReceiveContent(object o, EventArgs e)
		{
            //Get updated text from the socket
			Content = socket.content;
		}

		public void SendContent(string date, string html) 
		{
            //Send updated text to the socket
			socket.Key(html, date);
		}

		public void Join(string date) {
			socket.Join(date);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
