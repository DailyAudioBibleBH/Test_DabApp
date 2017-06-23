using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSettingsPage : DabBaseContentPage
	{
		public ViewCell offline { get { return _offline;} }
		public ViewCell reset { get { return _reset;} }
		public ViewCell appInfo { get { return _appInfo;} }
		public ViewCell profile { get { return _profile;} }
		public ViewCell addresses { get { return _addresses;} }
		ViewCell _offline;
		ViewCell _reset;
		ViewCell _appInfo;
		ViewCell _profile;
		ViewCell _addresses;

		public DabSettingsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
			_offline = Offline;
			_reset = Reset;
			_appInfo = AppInfo;
			_profile = Profile;
			_addresses = Addresses;
			if (GuestStatus.Current.IsGuestLogin)
			{
				logOut.Clear();
				Listening.Clear();
				Account.Clear();
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

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			LogOut.IsEnabled = true;
		}

		void OnOffline(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabOfflineEpisodeManagementPage());
			}
		}

		void OnReset(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabResetListenedToStatusPage());
			}
		}

		void OnAppInfo(object o, EventArgs e)
		{
			if (Device.Idiom == TargetIdiom.Phone)
			{
				Navigation.PushAsync(new DabAppInfoPage());
			}
		}

		void OnProfile(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabProfileManagementPage());
			}
		}

		void OnAddresses(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabAddressManagementPage());
			}
		}
	}
}
