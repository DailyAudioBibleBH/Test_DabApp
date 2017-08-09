using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace DABApp
{
	public class JournalTracker : INotifyPropertyChanged
	{
		public ISocket socket;
		private string _Content;
		private bool _IsConnected = false;
		private bool _IsJoined = false;

		public static JournalTracker Current { get; private set;}

		static JournalTracker()
		{
			Current = new JournalTracker();
		}

		private JournalTracker()
		{
			socket = DependencyService.Get<ISocket>();
			_Content = socket.content;
			socket.contentChanged += OnContentChanged;
			Device.StartTimer(TimeSpan.FromMilliseconds(100), () => 
			{
				if (_IsConnected != socket.IsConnected)
				{
					IsConnected = socket.IsConnected;
				}
				if (_IsJoined != socket.IsJoined)
				{
					IsJoined = socket.IsJoined;
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

		public bool IsJoined
		{ 
			get {
				return _IsJoined;
			}
			set {
				_IsJoined = value;
				OnPropertyChanged("IsJoined");
			}
		}

		void OnContentChanged(object o, EventArgs e)
		{
			Content = socket.content;
		}

		public void Connect(string token)
		{
			socket.Connect(token);
		}

		public void Update(string date, string html) 
		{
			socket.Key(html, date);
		}

		public void Join(string date) {
			socket.Join(date);
		}

		public bool Open { get; set; } = true;

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
