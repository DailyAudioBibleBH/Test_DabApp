using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSettingsPage : DabBaseContentPage
	{
		public Button AppInfo { get { return _AppInfo;} }
		public Button Offline { get { return _Offline;} }
		public Button Reset { get { return _Reset;} }
		Button _AppInfo;
		Button _Offline;
		Button _Reset;

		public DabSettingsPage()
		{
			InitializeComponent();
			_AppInfo = appInfo;
			_Reset = ResetListenedStatus;
			_Offline = OfflineManagement;
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
			if (GuestStatus.Current.IsGuestLogin)
			{
				LogOut.IsVisible = false;
				OfflineManagement.IsVisible = false;
				ResetListenedStatus.IsVisible = false;
			}
			else
			{
				LogOut.IsVisible = true;
				OfflineManagement.IsVisible = true;
				ResetListenedStatus.IsVisible = true;
			}
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				ControlTemplate NoPlayerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
				ControlTemplate = NoPlayerBarTemplate;
			}
		}

		async void OnLogOut(object o, EventArgs e)
		{
			LogOut.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			if (await AuthenticationAPI.LogOut())
			{
				Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			}
			else
			{
				//await DisplayAlert("OH NO!", "Something went wrong, Sorry.", "OK");
				Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			}
		}

		void OnAppInfo(object o, EventArgs e)
		{
			if (Device.Idiom == TargetIdiom.Phone)
			{
				Navigation.PushAsync(new DabAppInfoPage());
			}
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			LogOut.IsEnabled = true;
		}

		void OnOfflineManagement(object o, EventArgs e)
		{
			if (Device.Idiom == TargetIdiom.Phone)
				Navigation.PushAsync(new DabOfflineEpisodeManagementPage());
		}

		void OnResetListenedTo(object o, EventArgs e)
		{
			if (Device.Idiom == TargetIdiom.Phone)
			{
				Navigation.PushAsync(new DabResetListenedToStatusPage());
			}
		}
	
	}
}
