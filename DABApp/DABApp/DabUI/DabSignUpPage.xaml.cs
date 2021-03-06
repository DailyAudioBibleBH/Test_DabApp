using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSignUpPage : DabBaseContentPage
	{
		bool _fromPlayer;
		bool _fromDonation;
		static DabGraphQlVariables variables = new DabGraphQlVariables(); //Instance used for websocket communication
		bool GraphQlLoginRequestInProgress = false;
		bool GraphQlLoginComplete = false;

		public DabSignUpPage(string emailInput, bool fromPlayer = false, bool fromDonation = false)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				Container.Padding = 100;
			}
			_fromPlayer = fromPlayer;
			_fromDonation = fromDonation;
			BindingContext = ContentConfig.Instance.blocktext;
			ToolbarItems.Clear();
			var tapper = new TapGestureRecognizer();
			tapper.NumberOfTapsRequired = 1;
			tapper.Tapped += (sender, e) =>
			{
				Navigation.PushAsync(new DabTermsAndConditionsPage());
			};
			Terms.GestureRecognizers.Add(tapper);
            Terms.FormattedText = TermsText;

			if (emailInput != null)
			{
				Email.Text = emailInput;
			}
			SignUp.IsEnabled = true;
            if (Device.RuntimePlatform == Device.Android)
            {
				Container.Padding = new Thickness(10, 10, 10, 30);
            }
		}
		
		public FormattedString TermsText
		{
			get
			{
				return new FormattedString
				{
					Spans =
			        {
				        new Span { Text = "By signing up I agree to the Daily Audio Bible ", ForegroundColor=Color.White },
				        new Span { Text = "Terms of Serivce", ForegroundColor=Color.Red }
			        }
				};
			}
		}

		async void OnSignUp(object o, EventArgs e)
		{
			if (SignUpValidation())
			{
				DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Registering your account...", true));
				var ql = await DabService.RegisterUser(FirstName.Text, LastName.Text, Email.Text, Password.Text);
				if (ql.Success)
                {
					SQLite.SQLiteAsyncConnection adb = DabData.AsyncDatabase;

					//switch to a connection with their token
					string token = ql.Data.payload.data.registerUser.token;
					//token was updated successfully
					var newUserData = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;
					newUserData.Token = token;
					newUserData.TokenCreation = DateTime.Now;
					await adb.InsertOrReplaceAsync(newUserData);
					await DabService.TerminateConnection();
					await DabService.InitializeConnection(token);
					//get user profile information and update it.
					ql = await Service.DabService.GetUserData();
					if (ql.Success == true)
					{
						//process user profile information
						var profile = ql.Data.payload.data.user;
						await DabServiceRoutines.UpdateUserProfile(profile);
					}
					DabUserInteractionEvents.WaitStopped(o, new EventArgs());

					Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
				}
				else
                {
					DabUserInteractionEvents.WaitStopped(o, new EventArgs());
					await DisplayAlert("Registration Failed", $"Registration Failed: {ql.ErrorMessage}","OK");
                }
			}
		}

		bool SignUpValidation()
		{
			if (string.IsNullOrWhiteSpace(FirstName.Text))
			{
				DisplayAlert("First Name is Required", null, "OK");
				Password.Text = "";
				PasswordAgain.Text = "";
				return false;
			}
			if (string.IsNullOrWhiteSpace(LastName.Text))
			{
				DisplayAlert("Last Name is Required", null, "OK");
				Password.Text = "";
				PasswordAgain.Text = "";
				return false;
			}
			if (string.IsNullOrWhiteSpace(Email.Text))
			{
				DisplayAlert("Email is Required", null, "OK");
				Password.Text = "";
				PasswordAgain.Text = "";
				return false;
			}
			else
			{
				if (!Regex.Match(Email.Text, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$").Success)
				{
					DisplayAlert("Email must be a valid email!", null, "OK");
					Password.Text = "";
					PasswordAgain.Text = "";
					return false;
				}
			}
			if (string.IsNullOrWhiteSpace(Password.Text))
			{
				DisplayAlert("Password is Required", null, "OK");
				Password.Text = "";
				PasswordAgain.Text = "";
				return false;
			}
			if (string.IsNullOrWhiteSpace(PasswordAgain.Text))
			{
				DisplayAlert("Re Enter Password is Required", null, "OK");
				Password.Text = "";
				PasswordAgain.Text = "";
				return false;
			}
			if (Password.Text != PasswordAgain.Text)
			{
				DisplayAlert("Passwords Do Not Match", null, "OK");
				Password.Text = "";
				PasswordAgain.Text = "";
				return false;
			}
			if (!Agreement.IsToggled)
			{
				DisplayAlert("Wait", "Please read and agree to the Daily Audio Bible Terms of Service.", "OK");
				return false;
			}
			return true;
		}

		void OnFirstNameCompleted(object o, EventArgs e)
		{
			LastName.Focus();
		}

		void OnPasswordCompleted(object o, EventArgs e)
		{
			PasswordAgain.Focus();
		}

		void OnLastNameCompleted(object o, EventArgs e)
		{
			Password.Focus();
		}
	}
}
