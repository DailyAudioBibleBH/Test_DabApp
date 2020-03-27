using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSignUpPage : DabBaseContentPage
	{
		bool _fromPlayer;
		bool _fromDonation;

		public DabSignUpPage(bool fromPlayer = false, bool fromDonation = false)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet) {
				Container.Padding = 100;
			}
			_fromPlayer = fromPlayer;
			_fromDonation = fromDonation;
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
					GuestStatus.Current.IsGuestLogin = false;
					if (_fromPlayer)
					{
						await Navigation.PopModalAsync();
					}
					else
					{
						if (_fromDonation)
						{
							var dons = await AuthenticationAPI.GetDonations();
							if (dons.Length == 1)
							{
								var url = await PlayerFeedAPI.PostDonationAccessToken();
								if (url.StartsWith("http"))
								{
									DependencyService.Get<IRivets>().NavigateTo(url);
								}
								else
								{
									await DisplayAlert("Error", url, "OK");
								}

								var nav = new NavigationPage(new DabChannelsPage());
								nav.SetValue(NavigationPage.BarBackgroundColorProperty, (Color)App.Current.Resources["TextColor"]);
								Application.Current.MainPage = nav;
								await Navigation.PopToRootAsync();
							}
							else
							{
								var nav = new NavigationPage(new DabManageDonationsPage(dons, true));
								nav.SetValue(NavigationPage.BarBackgroundColorProperty, (Color)App.Current.Resources["TextColor"]);
								Application.Current.MainPage = nav;
								await Navigation.PopToRootAsync();
							}
						}
						else
						{
							var nav = new NavigationPage(new DabChannelsPage());
							nav.SetValue(NavigationPage.BarBackgroundColorProperty, (Color)App.Current.Resources["TextColor"]);
							Application.Current.MainPage = nav;
							await Navigation.PopToRootAsync();
						}
					}
				}
				else
				{
					if (authentication.Contains("server"))
					{
						await DisplayAlert("Server Error", authentication, "OK");
					}
					else {
						if (authentication.Contains("Http"))
						{
							await DisplayAlert(authentication, "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
						}
						if (authentication.Contains("Email already"))
						{
							await DisplayAlert("Authentication Error", "This email already exists", "OK");
						}
						else {
							await DisplayAlert("Unexpected Error",$"An unexpected error has been occurred while processing your request. Please check your connection and try again. Technical details: {authentication}", "OK");
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
			else {
				if (!Regex.Match(Email.Text, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$").Success) {
					DisplayAlert("Email must be a valid email!", null, "OK");
					return false;
				}
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
