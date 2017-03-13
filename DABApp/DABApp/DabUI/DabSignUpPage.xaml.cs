using System;
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
			ToolbarItems.Clear();
			var tapper = new TapGestureRecognizer();
			tapper.NumberOfTapsRequired = 1;
			tapper.Tapped += (sender, e) => {
				Navigation.PushAsync(new DabTermsAndConditionsPage());
			};
			Terms.GestureRecognizers.Add(tapper);
			Terms.Text = "<div style='font-size:14px;'>By signing up I agree to the Daily Audio Bible <span style='color: #ff0000'>Terms of Service.</span></div>";
		}

		async void OnSignUp(object o, EventArgs e) {
			SignUp.IsEnabled = false;
			if (SignUpValidation())
			{
				switch (await AuthenticationAPI.CreateNewMember(FirstName.Text, LastName.Text, Email.Text, Password.Text))
				{
					case 0:
						Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
						Navigation.PopToRootAsync();
						break;
					case 1:
						DisplayAlert("Email invalid", "Sorry, but there is currently a user with same email already registered in the system.", "OK");
						break;
					case 2:
						DisplayAlert("Http Request Timed out", "Please check your internet connection if problem persists it may be something wrong on our end", "OK");
						break;
					case 3:
						DisplayAlert("OH NO!", "Something went wrong", "OK");
						break;
				}
			}
			SignUp.IsEnabled = true;
		}

		bool SignUpValidation() 
		{
			if (string.IsNullOrWhiteSpace(FirstName.Text)) {
				DisplayAlert("First Name is Required", null, "OK");
				return false;
			}
			if (string.IsNullOrWhiteSpace(LastName.Text))
			{
				DisplayAlert("Last Name is Required", null, "OK");
				return false;
			}
			if (string.IsNullOrWhiteSpace(Email.Text))
			{
				DisplayAlert("Email is Required", null, "OK");
				return false;
			}
			if (string.IsNullOrWhiteSpace(Password.Text))
			{
				DisplayAlert("Password is Required", null, "OK");
				return false;
			}
			if (!Agreement.IsToggled)
			{
				DisplayAlert("Wait", "Please read and agree to the Daily Audio Bible Terms of Service.", "OK");
				return false;
			}
			return true;
		}
	}
}
