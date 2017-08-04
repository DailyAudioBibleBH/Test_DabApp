using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabLoginPage : DabBaseContentPage
	{
		static bool _fromPlayer;
		static bool _fromDonation;

		public DabLoginPage(bool fromPlayer = false, bool fromDonation = false)
		{
			InitializeComponent();
			NavigationPage.SetHasNavigationBar(this, false);
			_fromPlayer = fromPlayer;
			_fromDonation = fromDonation;
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
				Navigation.PushAsync(new DabSignUpPage(_fromPlayer, _fromDonation));
			};
			SignUp.GestureRecognizers.Add(tapper);
			SignUp.Text = "<div style='font-size:15px;'>Don't have an account? <font color='#ff0000'>Sign Up</font></div>";
			if (Device.Idiom == TargetIdiom.Tablet) {
				Container.Padding = 100;
			}
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
			if (result == "Success")
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
							if (url.Contains("http://"))
							{
								DependencyService.Get<IRivets>().NavigateTo(url);
							}
							else
							{
								await DisplayAlert("Error", url, "OK");
							}
							NavigationPage _nav = new NavigationPage(new DabChannelsPage());
							_nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
							Application.Current.MainPage = _nav;
							Navigation.PopToRootAsync();
						}
						else
						{
							NavigationPage _navs = new NavigationPage(new DabManageDonationsPage(dons, true));
							_navs.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
							Application.Current.MainPage = _navs;
							await Navigation.PopToRootAsync();
							//NavigationPage nav = new NavigationPage(new DabManageDonationsPage(dons));
							//nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
							//await Navigation.PushModalAsync(nav);
						}
					}
					else
					{
						NavigationPage _nav = new NavigationPage(new DabChannelsPage());
						_nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
						Application.Current.MainPage = _nav;
						Navigation.PopToRootAsync();
					}
				}
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
						await DisplayAlert("Error", result, "OK");
					}
				}
				else
				{
					await DisplayAlert("Login Failed", result, "OK");
				}
			}
			Login.IsEnabled = true;
		}

		void OnForgot(object o, EventArgs e) {
			Navigation.PushAsync(new DabResetPasswordPage());
		}

		async void OnGuestLogin(object o, EventArgs e) {
			GuestLogin.IsEnabled = false;
			GuestStatus.Current.IsGuestLogin = true;
			await AuthenticationAPI.ValidateLogin("Guest", "", true);
			if (_fromPlayer)
			{
				await Navigation.PopModalAsync();
			}
			else
			{
				NavigationPage _nav = new NavigationPage(new DabChannelsPage());
				_nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
				Application.Current.MainPage = _nav;
				await Navigation.PopToRootAsync();
			}
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (AudioPlayer.Instance.IsPlaying) {
				AudioPlayer.Instance.Pause();
			}
			AudioPlayer.Instance.Unload();
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
