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
			Terms.Text = "<div style='font-size:14px;'>By signing up I agree to the Daily Audio Bible <font color='#ff0000'>Terms of Service.</font></div>";
		}

		async void OnSignUp(object o, EventArgs e) {
			SignUp.IsEnabled = false;
			if (SignUpValidation())
			{
				string authentication = await AuthenticationAPI.CreateNewMember(FirstName.Text, LastName.Text, Email.Text, Password.Text);
				if (string.IsNullOrEmpty(authentication))
				{
					Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
					Navigation.PopToRootAsync();
					GlobalResources.IsGuestLogin = false;
				}
				else{
					if (authentication.Contains("server"))
					{
						await DisplayAlert("Server Error", authentication, "OK");
					}
					else {
						if (authentication.Contains("Http"))
						{
							await DisplayAlert(authentication, "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
						}
						else {
							await DisplayAlert("App side Error", authentication, "OK");
						}
					}
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

		void OnFirstNameCompleted(object o, EventArgs e) {
			LastName.Focus();
		}

		void OnLastNameCompleted(object o, EventArgs e) {
			Email.Focus();
		}

		void OnEmailCompleted(object o, EventArgs e) {
			Password.Focus();
		}
	}
}
