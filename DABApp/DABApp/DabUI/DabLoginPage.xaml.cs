﻿using System;
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
			if (await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text))
			{
				Navigation.PushModalAsync(new NavigationPage(new DabChannelsPage()));
			}
			else {
				await DisplayAlert("Login Failed", "Password and Email WRONG!", "OK");
				Login.IsEnabled = true;
			}
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