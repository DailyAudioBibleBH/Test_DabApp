using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSignUpPage : DabBaseContentPage
	{
		public DabSignUpPage()
		{
			InitializeComponent();
			BindingContext = ContentConfig.Instance.blocktext;
			this.ToolbarItems.Clear();
			var tapper = new TapGestureRecognizer();
			tapper.NumberOfTapsRequired = 1;
			tapper.Tapped += (sender, e) => {
				Navigation.PushAsync(new DabTermsAndConditionsPage());
			};
			Terms.GestureRecognizers.Add(tapper);
			Terms.Text = "<div style='font-size:14px;'>By signing up I agree to the Daily Audio Bible <span style='color: #ff0000'>Terms of Service.</span></div>";
		}

		async void OnSignUp(object o, EventArgs e) {
			if (Agreement.IsToggled)
			{
				if (await AuthenticationAPI.CreateNewMember(FirstName.Text, LastName.Text, Email.Text, Password.Text))
				{
					Navigation.PushModalAsync(new NavigationPage(new DabChannelsPage()));
				}
				else
				{
					DisplayAlert("Error", "Something broke", "OK");
				}
			}
			else {
				DisplayAlert("Wait", "Please read and agree to the Daily Audio Bible Terms of Service.", "OK");
			}
		}
	}
}
