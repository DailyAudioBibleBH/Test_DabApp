using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace DABApp
{
	public class GuestStatus : INotifyPropertyChanged
	{	
		public event PropertyChangedEventHandler PropertyChanged;
		bool _IsGuestLogin = false;
		string _UserName = GlobalResources.GetUserName();
		Uri _AvatarUrl = new Uri("http://placehold.it/10x10");

		public static GuestStatus Current { get; private set; }

		static GuestStatus()
		{
			Current = new GuestStatus();
		}

		public bool IsGuestLogin
		{
			get
			{
				return _IsGuestLogin;
			}
			set
			{
				_IsGuestLogin = value;
				OnPropertyChanged("IsGuestLogin");
			}
		}

		public string UserName
		{
			get
			{
				return _UserName;
			}
			set
			{
				_UserName = value;
				OnPropertyChanged("UserName");
			}
		}

		public Uri AvatarUrl
		{
			get
			{
				if (string.IsNullOrEmpty(GlobalResources.GetUserAvatar()))
				{
					return _AvatarUrl;
				}
				else return _AvatarUrl = new Uri(GlobalResources.GetUserAvatar());
			}
			set
			{
				_AvatarUrl = value;
				OnPropertyChanged("AvatarUrl");
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class NegateBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return !(bool)value;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return !(bool)value;
		}
	}
}
