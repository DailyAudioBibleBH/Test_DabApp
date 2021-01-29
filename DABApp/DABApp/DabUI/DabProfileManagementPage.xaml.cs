using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using Newtonsoft.Json;
using SQLite;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabProfileManagementPage : DabBaseContentPage
	{
		DabGraphQlVariables variables = new DabGraphQlVariables();
		SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors
		public int popRequests = 0;


		public DabProfileManagementPage()
		{
			InitializeComponent();
            if (GlobalResources.ShouldUseSplitScreen) { NavigationPage.SetHasNavigationBar(this, false); }
			dbUserData user = GlobalResources.Instance.LoggedInUser;
			FirstName.Text = user.FirstName;
			LastName.Text = user.LastName;
			Email.Text = user.Email;
		}

		async void OnSave(object o, EventArgs e) 
		{
			if (Validation()) 
			{
				DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Saving your information...", true));
				dbUserData user = GlobalResources.Instance.LoggedInUser;
				bool okToClose = true;
				string oldFirstName = user.FirstName;
				string oldLastName = user.LastName;
				string oldEmail = user.Email;


				if (FirstName.Text != oldFirstName || LastName.Text != oldLastName || Email.Text != oldEmail)
                {
					var ql = await DabService.SaveUserProfile(FirstName.Text, LastName.Text, Email.Text);
					if (ql.Success)
					{
						var data = ql.Data.payload.data.updateUserFields;
						//token was updated successfully
						var userData = GlobalResources.Instance.LoggedInUser;
						userData.FirstName = data.firstName;
						userData.LastName = data.lastName;
						userData.Email = data.email;
						await adb.InsertOrReplaceAsync(userData);

						GraphQlUser newUser = new GraphQlUser(userData);
						DabServiceEvents.UserProfileChanged(newUser);
					} 
					else
                    {
						await DisplayAlert("User profile could not be changed", ql.ErrorMessage, "OK");
						okToClose = false;
                    }
				}

                if (CurrentPassword.Text != null && NewPassword.Text != null && ConfirmNewPassword.Text != null && NewPassword.Text == ConfirmNewPassword.Text)
                {
					var ql = await DabService.ChangePassword(CurrentPassword.Text, NewPassword.Text);
					if (ql.Success)
					{
						await DisplayAlert($"Password changed", "Your password has been changed.", "OK");
					} else
                    {
						await DisplayAlert($"Password change failed", ql.ErrorMessage, "OK");
						okToClose = false;
                    }

					CurrentPassword.Text = "";
					NewPassword.Text = "";
					ConfirmNewPassword.Text = "";
				}

				//close the form if done
				DabUserInteractionEvents.WaitStopped(o, new EventArgs());
				if (okToClose)
				{
					await Navigation.PopAsync();
				}

			}

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
