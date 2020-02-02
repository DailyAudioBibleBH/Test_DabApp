using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabUpdateAddressPage : DabBaseContentPage
	{
		bool isShipping;

		public DabUpdateAddressPage(Address address, Country[] countries, bool IsShipping)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet && Device.RuntimePlatform != "Android")
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			BindingContext = address;
			isShipping = IsShipping;
			if (IsShipping) {
				EmailAndPhone.IsVisible = false;
				Title.Text = "Shipping Address";
			}
			Country.ItemsSource = countries;
			Country.ItemDisplayBinding = new Binding("countryName");
			if (string.IsNullOrEmpty(address.country))
			{
				Country.SelectedItem = countries.Single(x => x.countryCode == "US");
			}
			else
			{
				Country.SelectedItem = countries.Single(x => x.countryCode == address.country);
			}
			Country setCountry = ((Country)Country.SelectedItem);
			if (setCountry.regions.Length == 0)
			{
				_Region.IsVisible = false;
			}
			else _Region.IsVisible = true;
			Regions.ItemsSource = setCountry.regions;
			Regions.ItemDisplayBinding = new Binding("regionName");
			RegionLabel.Text = setCountry.regionLabel;
			Regions.SelectedItem = setCountry.regions.SingleOrDefault(x => x.regionCode == address.state);
			CodeLabel.Text = setCountry.postalCodeLabel;
		}

		async void OnSave(object o, EventArgs e) 
		{
			if (Validation())
			{
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
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
				update.country = ((Country)Country.SelectedItem).countryCode;
				if (Regions.SelectedItem != null)
				{
					update.state = ((Region)Regions.SelectedItem).regionCode;
				}
				if (isShipping)
				{
					update.type = "shipping";
				}
				var result = await AuthenticationAPI.UpdateBillingAddress(update);
				if (result == "true")
				{
					await DisplayAlert("Success", "Address successfully updated", "OK");
					await Navigation.PopAsync();
				}
				else
				{
					await DisplayAlert("Error", result, "OK");
				}
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
			}
		}

		void OnCountrySelected(object o, EventArgs e) {
			Country newCountry = (Country)Country.SelectedItem;
			if (newCountry.regions.Length == 0)
			{
				_Region.IsVisible = false;
			}
			else _Region.IsVisible = true;
			Regions.ItemsSource = newCountry.regions;
			RegionLabel.Text = newCountry.regionLabel;
			CodeLabel.Text = newCountry.postalCodeLabel;
		}

		bool Validation() 
		{
			bool result = true;
			if (string.IsNullOrEmpty(FirstName.Text)) {
				result = false;
				FirstNameWarning.IsVisible = true;
			}
			if (string.IsNullOrEmpty(LastName.Text)) 
			{
				result = false;
				LastNameWarning.IsVisible = true;
			}
			if (Country.SelectedItem != null)
			{
				Country selected = (Country)Country.SelectedItem;
				if (selected.countryCode == "US")
				{
					if (string.IsNullOrEmpty(Address1.Text))
					{
						result = false;
						AddressWarning.IsVisible = true;
					}
					if (string.IsNullOrEmpty(City.Text))
					{
						result = false;
						CityWarning.IsVisible = true;
					}
					if (Regions.SelectedItem == null)
					{
						result = false;
						RegionWarning.IsVisible = true;
					}
					if (string.IsNullOrEmpty(Code.Text)) 
					{
						result = false;
						CodeWarning.IsVisible = true;
					}
				}
			}
			else 
			{
				CountryWarning.IsVisible = true;
				result = false;
			}
			if (result) 
			{
				FirstNameWarning.IsVisible = false;
				LastNameWarning.IsVisible = false;
				CountryWarning.IsVisible = false;
				AddressWarning.IsVisible = false;
				CityWarning.IsVisible = false;
				CodeWarning.IsVisible = false;
				RegionWarning.IsVisible = false;
			}
			return result;
		}
	}
}
