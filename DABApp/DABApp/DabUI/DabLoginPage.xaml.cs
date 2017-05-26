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
			ToolbarItems.Clear();
			var email = GlobalResources.GetUserEmail();
			if (email != "Guest" && !String.IsNullOrEmpty(email)){
				Email.Text = email;
			}
			if (Device.Idiom == TargetIdiom.Phone) {
				Logo.WidthRequest = 250;
				Logo.Aspect = Aspect.AspectFit;
			}
			var tapper = new TapGestureRecognizer();
			tapper.NumberOfTapsRequired = 1;
			tapper.Tapped += (sender, e) =>
			{
				Navigation.PushAsync(new DabSignUpPage());
			};
			SignUp.GestureRecognizers.Add(tapper);
			SignUp.Text = "<div style='font-size:15px;'>Don't have an account? <font color='#ff0000'>Sign Up</font></div>";
		}

		async void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
			//switch (await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text)) { 
			//	case 0:
			//		Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
			//		await Navigation.PopToRootAsync();
			//		break;
			//	case 1:
			//		await DisplayAlert("Login Failed", "Password and Email WRONG!", "OK");
			//		break;
			//	case 2:
			//		await DisplayAlert("Request Timed Out", "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
			//		break;
			//	case 3:
			//		await DisplayAlert("OH NO!", "Something wen't wrong!", "OK");
			//		break;
			//}
			var result = await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text);
			if (result == null)
			{
				Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
				await Navigation.PopToRootAsync();
				GlobalResources.IsGuestLogin = false;
			}
			else 
			{
				if (result.Contains("Error"))
				{
					if (result.Contains("Http"))
					{
						await DisplayAlert("Request Timed Out", "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
					}
					else { 
						await DisplayAlert("OH NO!", result, "OK");
					}
				}
				else
				{
					await DisplayAlert("Login Failed", result, "OK");
				}
			}
			Login.IsEnabled = true;
		}

		void OnSignUp(object o, EventArgs e) {
			Navigation.PushAsync(new DabSignUpPage());
		}

		void OnForgot(object o, EventArgs e) {
			Navigation.PushAsync(new DabResetPasswordPage());
		}

		async void OnGuestLogin(object o, EventArgs e) {
			GuestLogin.IsEnabled = false;
			GlobalResources.IsGuestLogin = true;
			await AuthenticationAPI.ValidateLogin("Guest", "", true);
			Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
			await Navigation.PopToRootAsync();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			AudioPlayer.Instance.IsInitialized = false;
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			Login.IsEnabled = true;
			GuestLogin.IsEnabled = true;
		}

		void OnCompleted(object sender, System.EventArgs e)
		{
			Password.Focus();
		}
	}
}
