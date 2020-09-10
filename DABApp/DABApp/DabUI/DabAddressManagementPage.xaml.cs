using System;
using System.Collections.Generic;
using System.Linq;
using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAddressManagementPage : DabBaseContentPage
	{
        public List<DabGraphQlAddress> addresses;
		public DabGraphQlAddress billingAddress;
		public DabGraphQlAddress shippingAddress;
		object source = new object();

        public DabAddressManagementPage()
		{
			InitializeComponent();
			if (GlobalResources.ShouldUseSplitScreen){
				NavigationPage.SetHasNavigationBar(this, false);
			}
		}

        async void OnBilling(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Getting Billing Address...", true));

			//get user addresses
			var result = await Service.DabService.GetAddresses();
			if (result.Success == false) throw new Exception(result.ErrorMessage);

			var results = result.Data.payload.data.addresses;
			addresses = results;

			billingAddress = new DabGraphQlAddress();
            foreach (var item in addresses)
            {
				if (item.type == "billing")
					billingAddress = item;
            }
			string CountrySettings = dbSettings.GetSetting("Country", "");
			Dictionary<string, string> countries = JsonConvert.DeserializeObject<Dictionary<string, string>>(CountrySettings);

			if (countries != null)
            {
                await Navigation.PushAsync(new DabUpdateAddressPage(billingAddress, countries, false));
            }
            else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
            GlobalResources.WaitStop();
		}

		async void OnShipping(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Getting Shipping Address...", true));

			//get user addresses
			var result = await Service.DabService.GetAddresses();
			if (result.Success == false) throw new Exception(result.ErrorMessage);

			var results = result.Data.payload.data.addresses;
			addresses = results;

			shippingAddress = new DabGraphQlAddress();
			foreach (var item in addresses)
			{
				if (item.type == "shipping")
					shippingAddress = item;
			}
			string CountrySettings = dbSettings.GetSetting("Country", "");
			Dictionary<string, string> countries = JsonConvert.DeserializeObject<Dictionary<string, string>>(CountrySettings);

			if (countries != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(shippingAddress, countries, true));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			GlobalResources.WaitStop();
		}
	}
}
