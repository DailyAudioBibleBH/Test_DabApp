using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DABApp.DabUI.BaseUI;
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

			Title.Text = UniversalCampaign.campaignTitle;
			Intervals.ItemsSource = intervalOptions;
			Intervals.SelectedIndex = intervalOptions.FindIndex(x => x == campaign.RecurringInterval);
			List<dbCreditCards> cards = adb.Table<dbCreditCards>().Where(x => x.cardStatus != "deleted" || x.cardStatus == null ).ToListAsync().Result;
			Cards.ItemsSource = cards;
			Cards.ItemDisplayBinding = new Binding() { Converter = new CardConverter()};
			dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.donationId == campaign.Id).FirstOrDefaultAsync().Result;

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
					DabUserInteractionEvents.WaitStarted(obj, new DabAppEventArgs("Checking for new episodes...", true));
					AmountWarning.IsVisible = false;
					var card = (dbCreditCards)Cards.SelectedItem;
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

							NavigationPage navPage = new NavigationPage(new DabChannelsPage());
							navPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
							Application.Current.MainPage = navPage;
						}

					}
					else
					{
						var updateResult = Service.DabService.UpdateDonation(Amount.Text, Intervals.SelectedItem.ToString(), card.cardWpId, _campaign.CampaignWpId, Next.Date.ToString("yyyy-MM-dd"));
                        if (updateResult)
                        {
							await DisplayAlert("Your donation is in the process of updating.", "It may take a minute for the app to reflect your changes.", "OK");

							NavigationPage navPage = new NavigationPage(new DabChannelsPage());
							navPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
							Application.Current.MainPage = navPage;
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
				await Navigation.PushAsync(new DabChannelsPage(), false);
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
	}
}
