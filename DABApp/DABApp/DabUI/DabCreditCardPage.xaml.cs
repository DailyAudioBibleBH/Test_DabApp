﻿using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabCreditCardPage : DabBaseContentPage
	{
		Card _card;

		public DabCreditCardPage(Card card = null)
		{
			InitializeComponent();
			var months = new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"};
			var years = new List<string>() { "2017" };
			Month.ItemsSource = months;
			Year.ItemsSource = years;
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (card != null) {
				_card = card;
				Title.Text = "Card Details";
				CVC.IsVisible = false;
				CVCLabel.IsVisible = false;
				Delete.IsVisible = true;
				Save.IsVisible = false;
				DeleteText.IsVisible = true;
				CardNumber.IsEnabled = false;
				CardNumber.Text = $"**** **** **** {card.last4}";
				Month.IsEnabled = false;
				Month.SelectedItem = card.exp_month.ToString();
				Year.IsEnabled = false;
			}
		}

		async void OnSave(object o, EventArgs e)
		{
			Save.IsEnabled = false;
			var sCard = new Card
			{
				fullNumber = CardNumber.Text,
				exp_month = Convert.ToInt32(Month.SelectedItem),
				exp_year = Convert.ToInt32(Year.SelectedItem),
				cvc = CVC.Text
			};
			var result = await DependencyService.Get<IStripe>().AddCard(sCard);
			if (result.card_token.Contains("Error"))
			{
				DisplayAlert("Error", result.card_token, "OK");
			}
			else
			{
				var Result = await AuthenticationAPI.AddCard(result);
				if (Result.Contains("code"))
				{
					await DisplayAlert("Error", Result, "OK");
				}
				else {
					await Navigation.PopAsync();
				}
			}
			Save.IsEnabled = true;
		}

		async void OnDelete(object o, EventArgs e) 
		{
			Delete.IsEnabled = false;
			var result = await AuthenticationAPI.DeleteCard(_card.id);
			if (result.Contains("true")) {
				await Navigation.PopAsync();
			}
			else {
                DisplayAlert("Error", result, "OK");
			}
			Delete.IsEnabled = true;
		}
	}
}