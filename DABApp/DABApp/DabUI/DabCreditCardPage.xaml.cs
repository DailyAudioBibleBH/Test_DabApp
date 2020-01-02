using System;
using System.Collections.Generic;
using System.Linq;
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
			Month.ItemsSource = months;
			int start;
			if (card != null) start = 2010;
			else start = DateTime.Now.Year;
			int end = (DateTime.Now.Year - start) + 50;
			List<string> years = Enumerable.Range(start, end).Select(x => x.ToString()).ToList();
			Year.ItemsSource = years;
			if (GlobalResources.ShouldUseSplitScreen){
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (card != null)
			{
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
				Year.SelectedItem = card.exp_year.ToString();
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
				await DisplayAlert("Error", result.card_token, "OK");
			}
			else
			{
				var Result = await AuthenticationAPI.AddCard(result);
				if (Result.Contains("code") || Result.Contains("Error"))
				{
					await DisplayAlert("Error", Result.Remove(0, 7), "OK");
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
                await DisplayAlert("Error", result, "OK");
			}
			Delete.IsEnabled = true;
		}
	}
}
