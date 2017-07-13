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
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}
	}
}
