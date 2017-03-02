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
			BindingContext = ContentConfig.Instance.blocktext;
		}

		async void OnReset(object o, EventArgs e) {
			if (Email.Text == Confirmation.Text)
			{
				string message = await AuthenticationAPI.ResetPassword(Email.Text);
				if (string.IsNullOrEmpty(message))
				{
					await DisplayAlert("Oh no.", "Something broke, Sorry", "OK");
				}
				else {
					await DisplayAlert("App side code successfully run", message, "OK");
					await Navigation.PopAsync();
				}
			}
			else {
				await DisplayAlert("Confimation Email does not match Email!", "Make sure your email matches with the confirmation email.", "OK");
			}
		}
	}
}
