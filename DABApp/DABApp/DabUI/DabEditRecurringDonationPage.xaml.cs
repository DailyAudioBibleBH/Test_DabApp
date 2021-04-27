using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEditRecurringDonationPage : DabBaseContentPage
	{
		dbUserCampaigns _campaign;
		static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


		public DabEditRecurringDonationPage(dbUserCampaigns campaign)
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

			//Find campaign info and options tied to it
			int wpid = _campaign.CampaignWpId;
			dbCampaigns UniversalCampaign = adb.Table<dbCampaigns>().Where(x => x.campaignWpId == wpid).FirstOrDefaultAsync().Result;
			int campId = UniversalCampaign.campaignId;
			List<dbCampaignHasPricingPlan> pricingPlans = adb.Table<dbCampaignHasPricingPlan>().Where(x => x.CampaignId == campId).ToListAsync().Result;
			List<string> intervalOptions = new List<string>();
            foreach (var item in pricingPlans)
            {
				string pricingPlanId = item.PricingPlanId.ToString();
				dbPricingPlans interval = adb.Table<dbPricingPlans>().Where(x => x.id == pricingPlanId).FirstOrDefaultAsync().Result;
                if (interval != null)
                {
					intervalOptions.Add(interval.type);
                }
            }

			//UI setup
			Title.Text = UniversalCampaign.campaignTitle;
			Intervals.ItemsSource = intervalOptions;
			Intervals.SelectedIndex = intervalOptions.FindIndex(x => x == campaign.RecurringInterval);
			List<dbCreditCards> cards = adb.Table<dbCreditCards>().Where(x => x.cardStatus != "deleted" || x.cardStatus == null).ToListAsync().Result;
			dbCreditCards addNewCard = new dbCreditCards { cardType = "Add New Card", cardStatus = "NewCardFunction" };
			cards.Add(addNewCard);
			Cards.ItemsSource = cards;
			Cards.ItemDisplayBinding = new Binding() { Converter = new CardConverter()};

			//Add credit card section
			AddCreditCardStack.IsVisible = false;

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
			int start = DateTime.Now.Year;
			int end = (DateTime.Now.Year - start) + 50;
			List<string> years = Enumerable.Range(start, end).Select(x => x.ToString()).ToList();
			Year.ItemsSource = years;

			//Finding card tied to donation
			string cardSourceId = campaign.Source;
			dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.cardId == cardSourceId).FirstOrDefaultAsync().Result;
			if (source != null)
			{
				string currencyAmount = GlobalResources.ToCurrency(campaign.Amount);
				Amount.Text = currencyAmount;
				Next.Date = Convert.ToDateTime(source.next);
				int cardId = Convert.ToInt32(campaign.Source);
				Cards.SelectedItem = cards.Single(x => x.cardWpId == cardId);
				Status.Text = campaign.Status;
			}
			else 
			{
				Update.Text = "Add";
				Amount.Text = UniversalCampaign.campaignSuggestedRecurringDonation.ToString();
				Cancel.IsVisible = false;
			}
		}

		async void OnUpdate(object o, EventArgs e) 
		{
			if (Validation())
			{
				var accept = await DisplayAlert($"Are you sure you want to update this donation?", "You can update this donation by selecting \"Yes\"", "Yes", "No");
                if (accept)
                {
					object obj = new object();
					DabUserInteractionEvents.WaitStarted(obj, new DabAppEventArgs("Updating your donation...", true));
					AmountWarning.IsVisible = false;
					dbCreditCards card = (dbCreditCards)Cards.SelectedItem;
                    if (card.cardStatus == "NewCardFunction")
                    {
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
							return;
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
									card = newCard;
								}
								catch (Exception ex)
								{
									await DisplayAlert("Error", "Your card and donation were not updated. Error: " + ex.Message, "OK");
									return;
								}
							}
							else
							{
								await DisplayAlert("Error", "Your card and donation were not updated. Error: " + Result.ErrorMessage, "OK");
								return;
							}
						}
					}
					string cardSourceId = _campaign.Source;
					dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.cardId == cardSourceId).FirstOrDefaultAsync().Result;

					if (source == null)
					{
						var createResult = await Service.DabService.CreateDonation(Amount.Text, Intervals.SelectedItem.ToString(), card.cardWpId, _campaign.CampaignWpId, null);
						if (createResult.Success == false)
						{
							await DisplayAlert("Error", $"Error: {createResult.ErrorMessage}", "OK");
						}
						else
						{
							await DisplayAlert("Success", "Successfully Added Donation", "OK");

							await Navigation.PushAsync(new DabChannelsPage());
						}

					}
					else
					{
						var updateResult = Service.DabService.UpdateDonation(Amount.Text, Intervals.SelectedItem.ToString(), card.cardWpId, _campaign.CampaignWpId, Next.Date.ToString("yyyy-MM-dd"));
                        if (updateResult)
                        {
							await DisplayAlert("Your donation is in the process of updating.", "It may take a minute for the app to reflect your changes.", "OK");

							await Navigation.PushAsync(new DabChannelsPage());
						}
                        else
                        {
							await DisplayAlert("Your donation update did not get sent.", "You are not connected to the Daily Audio Bible service. If problem persists try logging out and logging back in again. ", "OK");
						}
					}
					DabUserInteractionEvents.WaitStopped(obj, new EventArgs());
				}
			}
			else 
			{
				AmountWarning.IsVisible = true;
			}
		}

		async void OnCancel(object o, EventArgs e) 
		{
			var accept = await DisplayAlert($"Are you sure you want to cancel this donation?", "You can cancel this donation by selecting \"Yes\"", "Yes", "No");

            if (accept)
            {
				Service.DabService.DeleteDonation(_campaign.CampaignWpId);
				await DisplayAlert("Your donation is in the process of being deleted.", "It may take a minute for the app to reflect your changes.", "OK");
				await Navigation.PushAsync(new DabSettingsPage());
			}
		}

		bool Validation() 
		{
			string a = @"^\$?\-?([1-9]{1}[0-9]{0,2}(\,\d{3})*(\.\d{0,2})?|[1-9]{1}\d{0,}(\.\d{0,2})?|0(\.\d{0,2})?|(\.\d{1,2}))$|^\-?\$?([1-9]{1}\d{0,2}(\,\d{3})*(\.\d{0,2})?|[1-9]{1}\d{0,}(\.\d{0,2})?|0(\.\d{0,2})?|(\.\d{1,2}))$|^\(\$?([1-9]{1}\d{0,2}(\,\d{3})*(\.\d{0,2})?|[1-9]{1}\d{0,}(\.\d{0,2})?|0(\.\d{0,2})?|(\.\d{1,2}))\)$";
			Regex rg = new Regex(a);

			if (rg.IsMatch(Amount.Text))
			{
				return true;
			}
			else 
			{
				return false;
			}
		}

        private void OnCardChanged(object sender, EventArgs e)
        {
			Picker picker = sender as Picker;
			dbCreditCards selectedItem = (dbCreditCards)picker.SelectedItem;
			if (selectedItem.cardStatus == "NewCardFunction")
            {
				AddCreditCardStack.IsVisible = true;
			}
            else
            {
				AddCreditCardStack.IsVisible = false;
				CardNumber.Text = "";
				Month.SelectedIndex = -1;
				Year.SelectedIndex = -1;
				entMonth.Text = "";
				entYear.Text = "";
				CVV.Text = "";
			}
		}
    }
}
