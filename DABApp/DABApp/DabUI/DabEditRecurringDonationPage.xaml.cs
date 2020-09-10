﻿using DABApp.DabUI.BaseUI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEditRecurringDonationPage : DabBaseContentPage
	{
		Donation _campaign;

		public DabEditRecurringDonationPage(Donation campaign, Card[] cards)
		{
			InitializeComponent();
			if (Device.RuntimePlatform != "Android")
			{
				base.ToolbarItems.RemoveAt(ToolbarItems.Count - 1);
			}
			else { 
				MessagingCenter.Send<string>("Remove", "Remove");
			}
			_campaign = campaign;
			Next.MinimumDate = DateTime.Now.AddDays(1);
			if (GlobalResources.ShouldUseSplitScreen)
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			Title.Text = campaign.name;
			Cards.ItemsSource = cards;
			Cards.ItemDisplayBinding = new Binding() { Converter = new CardConverter()};
			if (campaign.pro != null)
			{
				Amount.Text = campaign.pro.amount.ToString();
				Next.Date = Convert.ToDateTime(campaign.pro.next);
				Cards.SelectedItem = cards.Single(x => x.id == campaign.pro.card_id);
				Status.Text = campaign.pro.status;
			}
			else 
			{
				Update.Text = "Add";
				Amount.Text = campaign.suggestedRecurringDonation;
				Cancel.IsVisible = false;
			}
		}

		async void OnUpdate(object o, EventArgs e) 
		{
			if (Validation())
			{
				AmountWarning.IsVisible = false;
				DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));

				var card = (Card)Cards.SelectedItem;
				var stime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				long unix = (long)(Next.Date - stime).TotalSeconds;
				string result;
				if (_campaign.pro == null)
				{
					var address = await AuthenticationAPI.GetAddresses();
					var billing = address.billing;
					postDonation send;
					if (billing.country == "USA")
					{
						send = new postDonation(_campaign.id, card.id, Amount.Text, unix, billing.country, billing.address_1, billing.address_2, billing.city, billing.state);
					}
					else
					{
						send = new postDonation(_campaign.id, card.id, Amount.Text, unix, billing.country);
					}
					result = await AuthenticationAPI.AddDonation(send);
				}
				else
				{
					putDonation send = new putDonation(_campaign.id, card.id, Amount.Text, unix);
					result = await AuthenticationAPI.UpdateDonation(send);
				}
				if (result == "Success")
				{
					await DisplayAlert("Successfully Updated Donation", null, "OK");
					await Navigation.PopAsync();
				}
				else
				{
					await DisplayAlert("Error", result, "OK");
				}
				GlobalResources.WaitStop();
			}
			else 
			{
				AmountWarning.IsVisible = true;
			}
		}

		async void OnCancel(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));

			var decision = await DisplayAlert("Cancelling Donation", "Are you sure yout want to cancel your donation?", "Yes", "No");
			if (decision) {
				var result = await AuthenticationAPI.DeleteDonation(_campaign.id);
				if (result == "true")
				{
					await DisplayAlert("Successfully Deleted Donation", null, "OK");
					await Navigation.PopAsync();
				}
				else
				{
					await DisplayAlert("Error", result, "OK");
				}
			}
			GlobalResources.WaitStop();
		}

		bool Validation() 
		{
			if (Amount.Text.Contains("."))
			{
				return false;
			}
			else 
			{
				return true;
			}
		}
	}
}
