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
		}

		async void OnLogOut(object o, EventArgs e) {
			LogOut.IsEnabled = false;
			if (await AuthenticationAPI.LogOut())
			{
				if (GlobalResources.LogInPageExists)
				{
					Navigation.PopModalAsync();
				}
				else
				{
					Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
				}
			}
			else {
				await DisplayAlert("OH NO!", "Something went wrong, Sorry.", "OK");
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
	}
}
