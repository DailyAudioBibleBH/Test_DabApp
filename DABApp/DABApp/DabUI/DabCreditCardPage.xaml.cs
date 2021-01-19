using System;
using System.Collections.Generic;
using System.Globalization;
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
			var months = new List<string>() { "1 - " + new DateTime(2020, 1, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "2 - " + new DateTime(2020, 2, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "3 - " + new DateTime(2020, 3, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "4 - " + new DateTime(2020, 4, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "5 - " + new DateTime(2020, 5, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "6 - " + new DateTime(2020, 6, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "7 - " + new DateTime(2020, 7, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "8 - " + new DateTime(2020, 8, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "9 - " + new DateTime(2020, 9, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "10 - " + new DateTime(2020, 10, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "11 - " + new DateTime(2020, 11, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture), "12 - " + new DateTime(2020, 12, 1)
				.ToString("MMM", CultureInfo.CurrentUICulture)};
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
				CVV.IsVisible = false;
				CVCLabel.IsVisible = false;
				Delete.IsVisible = true;
                if (card.cardStatus == "deleted")
                {
					Delete.IsEnabled = false;
                }
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
			string[] selectedMonthArray = Month.SelectedItem.ToString().Split(' ');
			string selectedMonth = selectedMonthArray[0];
			var sCard = new Card
			{
				fullNumber = CardNumber.Text,
				exp_month = Convert.ToInt32(selectedMonth),
				exp_year = Convert.ToInt32(Year.SelectedItem),
				cvc = CVV.Text
			};
			var result = await DependencyService.Get<IStripe>().AddCard(sCard);
			if (result.card_token.Contains("Error"))
			{
				await DisplayAlert("Error", result.card_token, "OK");
			}
			else
			{
				var Result = await DabService.AddCard(result);
				if (Result.Success)
				{
                    try
                    {
						dbCreditCards newCard = new dbCreditCards(Result.Data.payload.data.updatedCard.card);
						await adb.InsertOrReplaceAsync(newCard);
						await DisplayAlert("Success", "Your card was successfully added", "OK");
						await Navigation.PopAsync();
					}
                    catch (Exception ex)
                    {
						await DisplayAlert("Error", "Your card was not saved. Error: " + ex.Message, "OK");
					}
				}
				else {
					await DisplayAlert("Error", "Your card was not saved. Error: " + Result.ErrorMessage, "OK");
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
					await DisplayAlert("Error", $"Card was not deleted. {result.ErrorMessage}", "OK");
				}
			}
			Delete.IsEnabled = true;
		}

  //      void Month_SelectedIndexChanged(System.Object sender, System.EventArgs e)
  //      {
		//	string[] selectedMonthArray = Month.SelectedItem.ToString().Split(' ');
		//	string selectedMonth = selectedMonthArray[0];

		//	Month.SelectedItem = selectedMonth;
		//}
    }
}
