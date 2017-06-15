using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabResetPasswordPage : DabBaseContentPage
	{
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
			if (Email.Text == Confirmation.Text)
			{
				string message = await AuthenticationAPI.ResetPassword(Email.Text);
				if (message.Contains("exception"))
				{
					await DisplayAlert("Error", message, "OK");
				}
				else {
					await DisplayAlert("App side code successfully run", message, "OK");
					await Navigation.PopAsync();
				}
			}
			else {
				await DisplayAlert("Confimation Email does not match Email!", "Make sure your email matches with the confirmation email.", "OK");
			}
			ResetPassword.IsEnabled = true;
		}

		void OnCompleted(object o, EventArgs e) {
			Confirmation.Focus();
		}
	}
}
