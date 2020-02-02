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
			if (Device.Idiom == TargetIdiom.Tablet && Device.RuntimePlatform != "Android"){
				NavigationPage.SetHasNavigationBar(this, false);
			}
		}

		async void OnBilling(object o, EventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var result = await AuthenticationAPI.GetAddresses();
			var countries = await AuthenticationAPI.GetCountries();
			if (result != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(result.billing, countries, false));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}

		async void OnShipping(object o, EventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var result = await AuthenticationAPI.GetAddresses();
			var countries = await AuthenticationAPI.GetCountries();
			if (result != null)
			{
				await Navigation.PushAsync(new DabUpdateAddressPage(result.shipping, countries, true));
			}
			else await DisplayAlert("Unable to retrieve Address information", "This might be due to a loss of internet connectivity.  Please check your internet connection and try again.", "OK");
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}
	}
}
