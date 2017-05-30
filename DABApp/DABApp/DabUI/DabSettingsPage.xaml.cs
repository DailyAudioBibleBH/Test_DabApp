using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSettingsPage : DabBaseContentPage
	{
		public DabSettingsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
			if (GuestStatus.Current.IsGuestLogin)
			{
				OfflineManagement.IsVisible = false;
				ResetListenedStatus.IsVisible = false;
			}
			else {
				OfflineManagement.IsVisible = true;
				ResetListenedStatus.IsVisible = true;
			}
		}

		async void OnLogOut(object o, EventArgs e) {
			LogOut.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			if (await AuthenticationAPI.LogOut())
			{
				Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			}
			else {
				//await DisplayAlert("OH NO!", "Something went wrong, Sorry.", "OK");
				Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			}
		}

		void OnAppInfo(object o, EventArgs e) {
			Navigation.PushAsync(new DabAppInfoPage());
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			LogOut.IsEnabled = true;
		}

		void OnOfflineManagement(object o, EventArgs e) {
			Navigation.PushAsync(new DabOfflineEpisodeManagementPage());
		}

		void OnResetListenedTo(object o, EventArgs e) {
			Navigation.PushAsync(new DabResetListenedToStatusPage());
		}
	}
}
