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
        int TapNumber = 0;
        private double _width;
        private double _height;

        public DabLoginPage(bool fromPlayer = false, bool fromDonation = false)
		{
			InitializeComponent();
            _width = this.Width;
            _height = this.Height;
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Logo.WidthRequest = GlobalResources.Instance.ScreenSize < 1000 ? 300 : 400;
            }
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
            SignUp.IsSelectable = false;
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
            //MessagingCenter.Subscribe<string>("OptimizationWarning", "OptimizationWarning", (obj) => {
            //    DisplayAlert("Background Playback", "This app needs to disable some battery optimization features to accommodate playback when your device goes to sleep. Please tap 'Yes' on the following prompt to give this permission.", "OK");
            //});
		}

		async void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
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
				MessagingCenter.Send<string>("Setup", "Setup");
				GuestStatus.Current.IsGuestLogin = false;
                await AuthenticationAPI.GetMemberData();
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
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
		}

		void OnForgot(object o, EventArgs e) {
			Navigation.PushAsync(new DabResetPasswordPage());
		}

		async void OnGuestLogin(object o, EventArgs e) {
			GuestLogin.IsEnabled = false;
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
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
            activity.IsVisible = false;
            activity.IsVisible = false;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (AudioPlayer.Instance.IsPlaying) {
				AudioPlayer.Instance.Pause();
			}
			AudioPlayer.Instance.Unload();
            TapNumber = 0;
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

        async void OnTest(object sender, EventArgs e)
        {
            TapNumber++;
            if (TapNumber >= 5)
            {
                var testprod = GlobalResources.TestMode ? "production" : "test";
                var accept = await DisplayAlert($"Do you want to switch to {testprod} mode?", "You will have to restart the app after selecting \"Yes\"", "Yes", "No");
                if (accept)
                {
                    GlobalResources.TestMode = !GlobalResources.TestMode;
                    AuthenticationAPI.SetTestMode();
                    await DisplayAlert($"Switching to {testprod} mode.", $"Please restart the app after receiving this message to fully go into {testprod} mode.", "OK");
                    Login.IsEnabled = false;
                    GuestLogin.IsEnabled = false;
                    SignUp.IsEnabled = false;
                }
            }
        }

        //protected override void OnSizeAllocated(double width, double height)
        //{
        //    double oldwidth = _width;
        //    base.OnSizeAllocated(width, height);
        //    if (Equals(_width, width) && Equals(_height, height)) return;
        //    _width = width;
        //    _height = height;
        //    if (Equals(oldwidth, -1)) return;
        //    if (width > height)
        //    {
        //        Logo.WidthRequest = Device.RuntimePlatform == "Android" ? 300 : 400;
        //        Logo.VerticalOptions = LayoutOptions.Start;
        //    }
        //    else
        //    {
        //        Logo.VerticalOptions = LayoutOptions.EndAndExpand;
        //    }
        //}
    }
}
