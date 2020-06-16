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
		SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


		public DabProfileManagementPage()
		{
			InitializeComponent();
            if (GlobalResources.ShouldUseSplitScreen) { NavigationPage.SetHasNavigationBar(this, false); }
			var UserName = GlobalResources.GetUserName().Split(' ');
			FirstName.Text = UserName[0];
			LastName.Text = UserName[1];
			Email.Text = GlobalResources.GetUserEmail();
		}

		void OnSave(object o, EventArgs e) 
		{
			Save.IsEnabled = false;
			if (Validation()) 
			{
				GlobalResources.WaitStart("Saving your information...");
				dbSettings FirstNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
				dbSettings LastNameSettings = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
				dbSettings EmailSettings = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;

                if (FirstName.Text != FirstNameSettings.Value || LastName.Text != LastNameSettings.Value || Email.Text != EmailSettings.Value)
                {
					var updateUserSettingsMutation = $"mutation {{ updateUserFields(firstName: \"{FirstName.Text}\", lastName: \"{LastName.Text}\", email: \"{Email.Text}\") {{ id wpId firstName lastName nickname email language channel channels userRegistered token }}}}";
					var updateUserSettingsPayload = new DabGraphQlPayload(updateUserSettingsMutation, variables);
					var settingsJson = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", updateUserSettingsPayload));
					DabSyncService.Instance.Send(settingsJson);
				}

                if (CurrentPassword.Text != null && NewPassword.Text != null && ConfirmNewPassword.Text != null)
                {
					var resetPasswordMutation = $"mutation {{ updatePassword( currentPassword: \"{CurrentPassword.Text}\" newPassword: \"{NewPassword.Text}\")}}";
					var resetPasswordPayload = new DabGraphQlPayload(resetPasswordMutation, variables);
					var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", resetPasswordPayload));
					DabSyncService.Instance.Send(JsonIn);

					CurrentPassword.Text = null;
					NewPassword.Text = null;
					ConfirmNewPassword.Text = null;
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
