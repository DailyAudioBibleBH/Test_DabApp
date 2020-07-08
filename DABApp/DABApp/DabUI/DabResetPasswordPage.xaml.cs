using DABApp.DabSockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabResetPasswordPage : DabBaseContentPage
	{
		DabGraphQlVariables variables = new DabGraphQlVariables();

		public DabResetPasswordPage()
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet) {
				Container.Padding = 100;
			}
			BindingContext = ContentConfig.Instance.blocktext;
			ToolbarItems.Clear();
		}

		async void OnReset(object o, EventArgs e) {
			ResetPassword.IsEnabled = false;

			//TODO: Test and implement
			var result = await Service.DabService.ResetPassword(Email.Text);
			if (result.Success == true)
            {
				await DisplayAlert("Password Reset", "Instructions to reset your password have been sent to your email.", "OK");
            } else
            {
				await DisplayAlert("Password Reset Failed", result.ErrorMessage, "OK");
            }

			ResetPassword.IsEnabled = true;
		}

		void OnCompleted(object o, EventArgs e) {
			Confirmation.Focus();
		}
	}
}
