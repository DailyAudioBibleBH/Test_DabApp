﻿using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSignUpPage : DabBaseContentPage
	{
		public DabSignUpPage()
		{
			InitializeComponent();
			BindingContext = ContentConfig.Instance.blocktext;
		}

		async void OnSignUp(object o, EventArgs e) {
			if (await AuthenticationAPI.CreateNewMember(FirstName.Text, LastName.Text, Email.Text, Password.Text))
			{
				Navigation.PushModalAsync(new NavigationPage(new DabChannelsPage()));
			}
			else {
				DisplayAlert("Error", "Something broke", "OK");
			}
		}
	}
}
