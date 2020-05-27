using DABApp.DabSockets;
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
			DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;
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
			Terms.Text = "<div style='font-size:14px;'>By signing up I agree to the Daily Audio Bible </br> <font color='#ff0000'>Terms of Service.</font></div>";

			if (emailInput != null)
			{
				Email.Text = emailInput;
			}
		}

		async void OnSignUp(object o, EventArgs e)
		{
			SignUp.IsEnabled = false;
			if (SignUpValidation())
			{
				GlobalResources.WaitStart("Checking your credentials...");
				string registerMutation = $"mutation {{registerUser(email: \"{Email.Text}\", firstName: \"{FirstName.Text}\", lastName: \"{LastName.Text}\", password: \"{Password.Text}\"){{ id wpId firstName lastName nickname email language channel channels userRegistered token }}}}";
				var mRegister = new DabGraphQlPayload(registerMutation, variables);
				DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", mRegister)));

				//	string authentication = await AuthenticationAPI.CreateNewMember(FirstName.Text, LastName.Text, Email.Text, Password.Text);
				//	if (string.IsNullOrEmpty(authentication))
				//	{
				//		GuestStatus.Current.IsGuestLogin = false;
				//		if (_fromPlayer)
				//		{
				//			await Navigation.PopModalAsync();
				//		}
				//		else
				//		{
				//			if (_fromDonation)
				//			{
				//				var dons = await AuthenticationAPI.GetDonations();
				//				if (dons.Length == 1)
				//				{
				//					var url = await PlayerFeedAPI.PostDonationAccessToken();
				//					if (url.StartsWith("http"))
				//					{
				//						DependencyService.Get<IRivets>().NavigateTo(url);
				//					}
				//					else
				//					{
				//						await DisplayAlert("Error", url, "OK");
				//					}
				//                             //user is logged in
				//					GlobalResources.Instance.IsLoggedIn = true;
				//					var nav = new NavigationPage(new DabChannelsPage());
				//					nav.SetValue(NavigationPage.BarBackgroundColorProperty, (Color)App.Current.Resources["TextColor"]);
				//					Application.Current.MainPage = nav;
				//					await Navigation.PopToRootAsync();
				//				}
				//				else
				//				{
				//                             //user is logged in
				//					GlobalResources.Instance.IsLoggedIn = true;
				//					var nav = new NavigationPage(new DabManageDonationsPage(dons, true));
				//					nav.SetValue(NavigationPage.BarBackgroundColorProperty, (Color)App.Current.Resources["TextColor"]);
				//					Application.Current.MainPage = nav;
				//					await Navigation.PopToRootAsync();
				//				}
				//			}
				//			else
				//			{
				//                         //user is logged in
				//				GlobalResources.Instance.IsLoggedIn = true;
				//				var nav = new NavigationPage(new DabChannelsPage());
				//				nav.SetValue(NavigationPage.BarBackgroundColorProperty, (Color)App.Current.Resources["TextColor"]);
				//				Application.Current.MainPage = nav;
				//				await Navigation.PopToRootAsync();
				//			}
				//		}
				//	}
				//	else
				//	{
				//		if (authentication.Contains("server"))
				//		{
				//			await DisplayAlert("Server Error", authentication, "OK");
				//		}
				//		else {
				//			if (authentication.Contains("Http"))
				//			{
				//				await DisplayAlert(authentication, "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
				//			}
				//			if (authentication.Contains("Email already"))
				//			{
				//				await DisplayAlert("Authentication Error", "This email already exists", "OK");
				//			}
				//			else {
				//				await DisplayAlert("Unexpected Error",$"An unexpected error has been occurred while processing your request. Please check your connection and try again. Technical details: {authentication}", "OK");
				//			}
				//		}
				//	}
				//}
				SignUp.IsEnabled = true;
			}
			else
			{
				SignUp.IsEnabled = true;
			}
		}

		bool SignUpValidation()
		{
			if (string.IsNullOrWhiteSpace(FirstName.Text))
			{
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
			else
			{
				if (!Regex.Match(Email.Text, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$").Success)
				{
					DisplayAlert("Email must be a valid email!", null, "OK");
					return false;
				}
			}
			if (string.IsNullOrWhiteSpace(Password.Text))
			{
				DisplayAlert("Password is Required", null, "OK");
				return false;
			}
			if (string.IsNullOrWhiteSpace(PasswordAgain.Text))
			{
				DisplayAlert("Re Enter Password is Required", null, "OK");
				return false;
			}
			if (Password.Text != PasswordAgain.Text)
			{
				DisplayAlert("Passwords Do Not Match", null, "OK");
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
			Email.Focus();
		}

		void OnEmailCompleted(object o, EventArgs e)
		{
			Password.Focus();
		}

		private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
		{
			//if (GraphQlLoginComplete)
			//{
			//    return; //get out of here once login is complete;
			//}

			Device.InvokeOnMainThreadAsync(async () =>
			{

				if (DabSyncService.Instance.IsConnected)
				{
					SQLiteAsyncConnection adb = DabData.AsyncDatabase;

					//Message received from the Graph QL - deal with those related to login messages!
					try
					{
						var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

						//Generic keep alive
						if (root.type == "ka")
						{
							//Nothing to see here...
							return;
						}
						if (root?.payload?.data?.registerUser != null)
						{
							try
							{
								//Login.IsEnabled = false;
								GlobalResources.WaitStart("Checking your credentials...");
								var result = await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text); //Sends message off to GraphQL
								if (result == "Request Sent")
								{
									//Wait for the reply from GraphQl before proceeding.
									GraphQlLoginRequestInProgress = true;
								}

								else
								{
									GlobalResources.WaitStop();
									if (result.Contains("Error"))
									{
										if (result.Contains("Http"))
										{
											await DisplayAlert("Request Timed Out", "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
										}
										else
										{
											await DisplayAlert("Error", "An unknown error occured while trying to log in. Please try agian.", "OK");
										}
									}
									else
									{
										await DisplayAlert("Login Failed", result, "OK");
									}
								}
								//Login.IsEnabled = true;
							}
							catch (Exception ex)
							{
								GlobalResources.WaitStop();
								Debug.WriteLine(ex.Message);
								await DisplayAlert("System Error", "System error with login. Try again or restart application.", "Ok");
								await Navigation.PushAsync(new DabCheckEmailPage());
							}
						}
						if (root?.payload?.data?.loginUser != null)
						{

							//Store the token
							dbSettings sToken = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
							if (sToken == null)
							{
								sToken = new dbSettings() { Key = "Token" };
							}
							sToken.Value = root.payload.data.loginUser.token;
							await adb.InsertOrReplaceAsync(sToken);

							//Update Token Life
							ContentConfig.Instance.options.token_life = 5;
							dbSettings sTokenCreationDate = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
							if (sTokenCreationDate == null)
							{
								sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
							}
							sTokenCreationDate.Value = DateTime.Now.ToString();
							await adb.InsertOrReplaceAsync(sTokenCreationDate);

							//Reset the connection with the new token
							DabSyncService.Instance.PrepConnectionWithTokenAndOrigin(sToken.Value);

							//Send a request for updated user data
							string jUser = $"query {{user{{wpId,firstName,lastName,email}}}}";
							var pLogin = new DabGraphQlPayload(jUser, new DabGraphQlVariables());
							DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", pLogin)));

						}
						else if (root?.payload?.data?.user != null)
						{
							//We got back user data!
							GraphQlLoginComplete = true; //stop processing success messages.
														 //Save the data
							dbSettings sEmail = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
							dbSettings sFirstName = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
							dbSettings sLastName = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
							dbSettings sAvatar = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
							dbSettings sWpId = adb.Table<dbSettings>().Where(x => x.Key == "WpId").FirstOrDefaultAsync().Result;
							if (sEmail == null) sEmail = new dbSettings() { Key = "Email" };
							if (sFirstName == null) sFirstName = new dbSettings() { Key = "FirstName" };
							if (sLastName == null) sLastName = new dbSettings() { Key = "LastName" };
							if (sAvatar == null) sAvatar = new dbSettings() { Key = "Avatar" };
							if (sWpId == null) sWpId = new dbSettings() { Key = "WpId" };
							sEmail.Value = root.payload.data.user.email;
							sFirstName.Value = root.payload.data.user.firstName;
							sLastName.Value = root.payload.data.user.lastName;
							sAvatar.Value = "https://www.gravatar.com/avatar/" + CalculateMD5Hash(GlobalResources.GetUserEmail()) + "?d=mp";
							sWpId.Value = root.payload.data.user.wpId.ToString();
							var x = adb.InsertOrReplaceAsync(sEmail).Result;
							x = adb.InsertOrReplaceAsync(sFirstName).Result;
							x = adb.InsertOrReplaceAsync(sLastName).Result;
							x = adb.InsertOrReplaceAsync(sAvatar).Result;
							x = adb.InsertOrReplaceAsync(sWpId).Result;

							GraphQlLoginRequestInProgress = false;

							GuestStatus.Current.IsGuestLogin = false;
							await AuthenticationAPI.GetMemberData();

							//user is logged in
							GlobalResources.WaitStop();
							GlobalResources.Instance.IsLoggedIn = true;
							DabChannelsPage _nav = new DabChannelsPage();
							_nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
							//Application.Current.MainPage = _nav;
							await Navigation.PushAsync(_nav);
							MessagingCenter.Send<string>("Setup", "Setup");

							//Delete nav stack so user cant back into login screen
							var existingPages = Navigation.NavigationStack.ToList();
							foreach (var page in existingPages)
							{
								Navigation.RemovePage(page);
							}
						}

						else if (root?.payload?.errors?.First() != null)
						{
							if (GraphQlLoginRequestInProgress == true)
							{
								GlobalResources.WaitStop();
								//We have a login error!
								await DisplayAlert("Login Error", root.payload.errors.First().message, "OK");
								GraphQlLoginRequestInProgress = false;
							}
						}
						else
						{
							//Some other GraphQL message we don't care about here.

						}
					}
					catch (Exception ex)
					{
						GlobalResources.WaitStop();
						System.Diagnostics.Debug.WriteLine(ex.Message);
						//Some other GraphQL message we don't care about here.

					}
				}
				else
				{
					GlobalResources.WaitStop();
					//DabSyncService.Instance.Init();
					DabSyncService.Instance.Connect();
				}
			});
		}

		public string CalculateMD5Hash(string email)
		{
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(email);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}
	}
}
