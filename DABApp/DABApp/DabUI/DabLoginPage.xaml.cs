using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabLoginPage : DabBaseContentPage
	{
		public DabLoginPage()
		{
			InitializeComponent();
			GlobalResources.LogInPageExists = true;
			NavigationPage.SetHasNavigationBar(this, false);
			var email = GlobalResources.GetUserEmail();
			if (!string.IsNullOrEmpty(email)) {
				Email.Text = email;
			}
			if (Device.Idiom == TargetIdiom.Phone) {
				Logo.WidthRequest = 250;
				Logo.Aspect = Aspect.AspectFit;
			}
		}

		async void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
			switch (await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text)) { 
				case 0:
					Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
					await Navigation.PopToRootAsync();
					break;
				case 1:
					await DisplayAlert("Login Failed", "Password and Email WRONG!", "OK");
					break;
				case 2:
					await DisplayAlert("Request Timed Out", "We are sorry this is most likely a problem on our end.", "OK");
					break;
				case 3:
					await DisplayAlert("OH NO!", "Something wen't wrong!", "OK");
					break;
			}
			Login.IsEnabled = true;
			//if (await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text))
			//{
			//	Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
			//	await Navigation.PopToRootAsync();	
			//}
			//else {
			//	await DisplayAlert("Login Failed", "Password and Email WRONG!", "OK");
			//	Login.IsEnabled = true;
			//}
		}

		void OnSignUp(object o, EventArgs e) {
			Navigation.PushAsync(new DabSignUpPage());
		}

		void OnForgot(object o, EventArgs e) {
			Navigation.PushAsync(new DabResetPasswordPage());
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			Login.IsEnabled = true;
		}
	}
}
