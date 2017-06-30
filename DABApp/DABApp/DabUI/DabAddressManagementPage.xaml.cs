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
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
		}

		async void OnBilling(object o, EventArgs e) 
		{
			var result = await AuthenticationAPI.GetAddresses();
			var countries = await AuthenticationAPI.GetCountries();
			if (result != null)
			{
				Navigation.PushAsync(new DabUpdateAddressPage(result.billing, countries, false));
			}
		}

		async void OnShipping(object o, EventArgs e) 
		{
			var result = await AuthenticationAPI.GetAddresses();
			var countries = await AuthenticationAPI.GetCountries();
			if (result != null)
			{
				Navigation.PushAsync(new DabUpdateAddressPage(result.shipping, countries, true));
			}
		}
	}
}
