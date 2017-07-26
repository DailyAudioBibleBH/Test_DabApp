using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace DABApp
{
	public class JournalTracker : INotifyPropertyChanged
	{
		private ISocket socket;
		private string _Content;


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

		void OnContentChanged(object o, EventArgs e)
		{
			Content = socket.content;
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
