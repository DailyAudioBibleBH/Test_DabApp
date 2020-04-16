using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAddressManagementPage : DabBaseContentPage
	{
		public DabAddressManagementPage()
		{
			InitializeComponent();
			if (GlobalResources.ShouldUseSplitScreen){
				NavigationPage.SetHasNavigationBar(this, false);
			}
		}

		async void OnBilling(object o, EventArgs e) 
		{
			GlobalResources.WaitStart("Getting Billing Address...");
			var result = await AuthenticationAPI.GetAddresses();
			var countries = await AuthenticationAPI.GetCountries();
			if (result != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(result.billing, countries, false));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			GlobalResources.WaitStop();
		}

		async void OnShipping(object o, EventArgs e) 
		{
			GlobalResources.WaitStart("Getting Shipping Address...");
			var result = await AuthenticationAPI.GetAddresses();
			var countries = await AuthenticationAPI.GetCountries();
			if (result != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(result.shipping, countries, true));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			GlobalResources.WaitStop();
		}
	}
}
