using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabProfileManagementPage : DabBaseContentPage
	{
		public DabProfileManagementPage()
		{
			InitializeComponent();
			var UserName = GlobalResources.GetUserName().Split(' ');
			FirstName.Text = UserName[0];
			LastName.Text = UserName[1];
			Email.Text = GlobalResources.GetUserEmail();
		}

		async void OnSave(object o, EventArgs e) 
		{
			Save.IsEnabled = false;
			if (Validation()) 
			{
				var message = await AuthenticationAPI.EditMember(Email.Text, FirstName.Text, LastName.Text, CurrentPassword.Text, NewPassword.Text, ConfirmNewPassword.Text);
				if (message == "Success")
				{
					DisplayAlert(message, "User profile information has been updated", "OK");
					Email.Text = GlobalResources.GetUserEmail();
					var UserName = GlobalResources.GetUserName().Split(' ');
					FirstName.Text = UserName[0];
					LastName.Text = UserName[1];
					CurrentPassword.Text = null;
					NewPassword.Text = null;
					ConfirmNewPassword.Text = null;
					GuestStatus.Current.UserName = GlobalResources.GetUserName();
				}
				else {
					DisplayAlert("An error has occured", message, "OK");
				}
			}
			Save.IsEnabled = true;
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
