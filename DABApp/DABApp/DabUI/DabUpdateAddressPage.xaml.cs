using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabUpdateAddressPage : DabBaseContentPage
	{
		bool isShipping;

		public DabUpdateAddressPage(Address address, bool IsShipping)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			BindingContext = address;
			isShipping = IsShipping;
			if (IsShipping) {
				EmailAndPhone.IsVisible = false;
				Title.Text = "Shipping Address";
			}
		}

		async void OnSave(object o, EventArgs e) {
			var update = new Address();
			update.first_name = FirstName.Text;
			update.last_name = LastName.Text;
			update.company = CompanyName.Text;
			update.email = Email.Text;
			update.phone = Phone.Text;
			update.address_1 = Address1.Text;
			update.address_2 = Address2.Text;
			update.city = City.Text;
			update.postcode = Code.Text;
			var result = await AuthenticationAPI.UpdateBillingAddress(update);
			if (result)
			{
				await DisplayAlert("Success", "Address successfully updated", "OK");
				await Navigation.PopAsync();
			}
			else {
				await DisplayAlert("Error", null, "OK");
			}
		}
	}
}
