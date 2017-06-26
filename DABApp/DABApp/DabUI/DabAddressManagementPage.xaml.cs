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
			if (result != null)
			{
				Navigation.PushAsync(new DabUpdateAddressPage(result.billing, false));
			}
		}

		async void OnShipping(object o, EventArgs e) 
		{
			DisplayAlert("We're sorry, but you cannot update your shipping address using the mobile app at this time. Please visit dailyaudiobible.com to update your shipping address.", null, "OK");
			//var result = await AuthenticationAPI.GetAddresses();
			//if (result != null)
			//{
			//	result.shipping.isShipping = true;
			//	Navigation.PushAsync(new DabUpdateAddressPage(result.shipping, true));
			//}
		}
	}
}
