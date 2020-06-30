using System;
using System.Collections.Generic;
using DABApp.DabSockets;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAddressManagementPage : DabBaseContentPage
	{
        public List<DabGraphQlAddress> addresses;
		public DabGraphQlAddress billingAddress;
		public DabGraphQlAddress shippingAddress;

        public DabAddressManagementPage(List<DabGraphQlAddress> userAddresses)
		{
			InitializeComponent();
			if (GlobalResources.ShouldUseSplitScreen){
				NavigationPage.SetHasNavigationBar(this, false);
			}
			this.addresses = userAddresses;
		}

        async void OnBilling(object o, EventArgs e) 
		{
			GlobalResources.WaitStart("Getting Billing Address...");
			billingAddress = new DabGraphQlAddress();
            foreach (var item in addresses)
            {
				if (item.type == "billing")
					billingAddress = item;
            }
			var countries = await AuthenticationAPI.GetCountries();
			if (countries != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(billingAddress, countries, false));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			GlobalResources.WaitStop();
		}

		async void OnShipping(object o, EventArgs e) 
		{
			GlobalResources.WaitStart("Getting Shipping Address...");
			shippingAddress = new DabGraphQlAddress();
			foreach (var item in addresses)
			{
				if (item.type == "shipping")
					shippingAddress = item;
			}
			var countries = await AuthenticationAPI.GetCountries();
			if (countries != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(shippingAddress, countries, true));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			GlobalResources.WaitStop();
		}
	}
}
