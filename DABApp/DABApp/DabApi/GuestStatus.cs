using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FFImageLoading;
using Xamarin.Forms;

namespace DABApp
{
	public class GuestStatus : INotifyPropertyChanged
	{	
		public event PropertyChangedEventHandler PropertyChanged;
		bool _IsGuestLogin = false;
		string _UserName = GlobalResources.GetUserName();
		string _AvatarUrl;
		ImageSource _AvatarSource;

		public static GuestStatus Current { get; private set; }

		static GuestStatus()
		{
			Current = new GuestStatus();
		}

		private GuestStatus() 
		{
			_AvatarUrl = "http://placehold.it/10x10";
			_AvatarSource = ImageSource.FromUri(new Uri(_AvatarUrl));
		}

		public bool IsGuestLogin
		{
			get
			{
				if (dbSettings.GetSetting("Token", "") == "")
				{
					return true;
				}
				else
				{
					return false;
				}
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

		public string AvatarUrl
		{
			get
			{
				return GlobalResources.UserAvatar;
			}
			set
			{
				_AvatarUrl = value;
				OnPropertyChanged("AvatarUrl");
			}
		}

		public ImageSource AvatarSource { 
			get {
				return _AvatarSource;
			}
			set {
				_AvatarSource = value;
				PropertyChanged(this, new PropertyChangedEventArgs("AvatarSource"));
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public event EventHandler AvatarChanged;
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
