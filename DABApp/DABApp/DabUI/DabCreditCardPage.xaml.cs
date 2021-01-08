using System;
using System.Collections.Generic;
using System.Linq;
using DABApp.Service;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabCreditCardPage : DabBaseContentPage
	{
		dbCreditCards _card;
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

		public DabCreditCardPage(dbCreditCards card = null)
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
			if (GlobalResources.ShouldUseSplitScreen)
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (card != null)
			{
				_card = card;
				Title.Text = "Card Details";
				CardType.Text = card.cardType;
				CardType.IsVisible = true;				
				//Month.SelectedIndex = months.IndexOf(card.cardExpMonth.ToString());
				//Year.SelectedIndex = months.IndexOf(card.cardExpYear.ToString());
				CVC.IsVisible = false;
				CVCLabel.IsVisible = false;
				Delete.IsVisible = true;
				Save.IsVisible = false;
				DeleteText.IsVisible = true;
				CardNumber.IsEnabled = false;
				CardNumber.Text = $"**** **** **** {card.cardLastFour}";
				Month.SelectedItem = card.cardExpMonth.ToString();
				Month.IsEnabled = false;
				Month.IsVisible = false;
				Year.SelectedItem = card.cardExpYear.ToString();
				Year.IsEnabled = false;
				Year.IsVisible = false;
				//added disabled entries since disabled pickers dont show values on ios
				entMonth.Text = card.cardExpMonth.ToString();
				entYear.Text = card.cardExpYear.ToString();
				entMonth.IsVisible = true;
				entYear.IsVisible = true;
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
			var accept = await DisplayAlert("Alert", "Are you sure you want to remove this card?", "Yes", "No");
			if (accept)
			{
				var result = await DabService.DeleteCard(_card.cardWpId);
				if (result.Success)
				{
					dbCreditCards card = adb.Table<dbCreditCards>().Where(x => x.cardWpId == _card.cardWpId).FirstOrDefaultAsync().Result;
					card.cardStatus = "deleted";
					await adb.UpdateAsync(card);
					await Navigation.PopAsync();
				}
				else
				{
					await DisplayAlert("Error", $"Card was not deleted. Error: {result.ErrorMessage}", "OK");
				}
			}
			Delete.IsEnabled = true;
		}
	}
}
