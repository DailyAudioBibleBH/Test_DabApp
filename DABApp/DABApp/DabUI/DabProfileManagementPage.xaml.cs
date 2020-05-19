using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DABApp.DabSockets;
using Newtonsoft.Json;
using SQLite;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabProfileManagementPage : DabBaseContentPage
	{
		DabGraphQlVariables variables = new DabGraphQlVariables();

		public DabProfileManagementPage()
		{
			InitializeComponent();
            if (GlobalResources.ShouldUseSplitScreen) { NavigationPage.SetHasNavigationBar(this, false); }
			var UserName = GlobalResources.GetUserName().Split(' ');
			FirstName.Text = UserName[0];
			LastName.Text = UserName[1];
			Email.Text = GlobalResources.GetUserEmail();
			DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;
		}

		async void OnSave(object o, EventArgs e) 
		{
			Save.IsEnabled = false;
			if (Validation()) 
			{
				var message = await AuthenticationAPI.EditMember(Email.Text, FirstName.Text, LastName.Text);
				if (message == "Success")
				{
					await DisplayAlert(message, "User profile information has been updated", "OK");
					Email.Text = GlobalResources.GetUserEmail();
					var UserName = GlobalResources.GetUserName().Split(' ');
					FirstName.Text = UserName[0];
					LastName.Text = UserName[1];
					GuestStatus.Current.UserName = GlobalResources.GetUserName();
					
				}
                if (CurrentPassword != null && NewPassword != null && ConfirmNewPassword != null)
                {
					GlobalResources.WaitStart();
					var resetPasswordMutation = $"mutation {{ updatePassword( currentPassword: \"{CurrentPassword.Text}\" newPassword: \"{NewPassword.Text}\")}}";
					var resetPasswordPayload = new DabGraphQlPayload(resetPasswordMutation, variables);
					var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", resetPasswordPayload));
					DabSyncService.Instance.Send(JsonIn);

					CurrentPassword.Text = null;
					NewPassword.Text = null;
					ConfirmNewPassword.Text = null;
				}
                if (message == "Success")
                {
					if (Device.Idiom == TargetIdiom.Phone)
					{
						await Navigation.PopAsync();
					}
				}
				else {
					await DisplayAlert("An error has occured", message, "OK");
				}
			}
			Save.IsEnabled = true;
		}

        private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            Device.InvokeOnMainThreadAsync(async () =>
            {

                if (DabSyncService.Instance.IsConnected)
                {
                    SQLiteAsyncConnection adb = DabData.AsyncDatabase;

                    //Message received from the Graph QL - deal with those related to login messages!
                    try
                    {
                        var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

						if (root.payload?.data?.updatePassword != null)
						{
							if (root.payload.data.updatePassword == true)
							{
								MainThread.BeginInvokeOnMainThread(() =>
								{
									Application.Current.MainPage.DisplayAlert("Success", "Password Successfully Updated", "OK");
								});
							}
						}

						else if (root?.payload?.errors?.First() != null)
                        {
                            //if (GraphQlLoginRequestInProgress == true)
                            //{
                            GlobalResources.WaitStop();
							//We have an error!
							MainThread.BeginInvokeOnMainThread(() =>
							{
								Application.Current.MainPage.DisplayAlert("Error", root.payload.errors.First().message, "OK");
							});
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

        void OnFirstNameCompleted(object o, EventArgs e) {
			LastName.Focus();
		}

		void OnLastNameCompleted(object o, EventArgs e) {
			Email.Focus();
		}

		void OnCurrentPasswordCompleted(object o, EventArgs e) {
			NewPassword.Focus();
		}

		void OnNewPasswordCompleted(object o, EventArgs e) {
			ConfirmNewPassword.Focus();
		}

		bool Validation() 
		{
			if (!string.IsNullOrWhiteSpace(CurrentPassword.Text) || !string.IsNullOrEmpty(NewPassword.Text) || !string.IsNullOrEmpty(ConfirmNewPassword.Text)) {
				if (string.IsNullOrEmpty(CurrentPassword.Text)) {
					DisplayAlert("Current Password is Required to change password", null, "OK");
					return false;
				}
				if (string.IsNullOrEmpty(NewPassword.Text)) { 
                  DisplayAlert("New Password is Required to change password", null, "OK");
					return false;
				}
				if (string.IsNullOrEmpty(ConfirmNewPassword.Text)) { 

					DisplayAlert("Confirmation of new password is Required to change password", null, "OK");
					return false;
				}
				if (NewPassword.Text != ConfirmNewPassword.Text) {
					DisplayAlert("New Password and Confirm New Password fields must match to change password", null, "OK");
					return false;
				}
			}
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
			return true;
		}
	}
}
