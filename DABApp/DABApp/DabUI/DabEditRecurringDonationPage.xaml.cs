﻿using System;
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


		public DabEditRecurringDonationPage(dbUserCampaigns campaign, List<dbCreditCards> cards)
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
			dbCampaigns UniversalCampaign = adb.Table<dbCampaigns>().Where(x => x.campaignWpId == _campaign.CampaignWpId).FirstOrDefaultAsync().Result;
			var test = adb.Table<dbCampaignHasPricingPlan>().ToListAsync().Result;

			List<dbCampaignHasPricingPlan> pricingPlans = adb.Table<dbCampaignHasPricingPlan>().Where(x => x.CampaignWpId == _campaign.CampaignWpId).ToListAsync().Result;
			List<string> intervalOptions = new List<string>();
            foreach (var item in pricingPlans)
            {
				string pricingPlanId = item.PricingPlanId.ToString();
				string interval = adb.Table<dbPricingPlans>().Where(x => x.id == pricingPlanId).FirstOrDefaultAsync().Result.type;
                if (interval != null)
                {
					intervalOptions.Add(interval);
                }
            }

			Title.Text = UniversalCampaign.campaignTitle;
			Intervals.ItemsSource = intervalOptions;
			//Intervals.SelectedItem = campaign.recurringIntervalOptions.Where(x => x.Equals(campaign.pro.interval));
			Intervals.SelectedIndex = intervalOptions.FindIndex(x => x == campaign.RecurringInterval);
			Cards.ItemsSource = cards;
			Cards.ItemDisplayBinding = new Binding() { Converter = new CardConverter()};
			dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.cardId == campaign.Source).FirstOrDefaultAsync().Result;
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
				AmountWarning.IsVisible = false;
				DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
				var card = (dbCreditCards)Cards.SelectedItem;
				dbCreditSource source = adb.Table<dbCreditSource>().Where(x => x.cardId == _campaign.Source).FirstOrDefaultAsync().Result;

				if (source == null)
				{
					var createResult = await Service.DabService.CreateDonation(Amount.Text, card.cardType, card.cardWpId, _campaign.CampaignWpId, null);
					if (createResult.Success == false) 
					{
						await DisplayAlert("Error", $"Error: {createResult.ErrorMessage}", "OK");
					}
					else
					{
						await DisplayAlert("Success", "Successfully Added Donation", "OK");
						await Navigation.PopAsync();
					}

				}
				else
				{
					var updateResult = await Service.DabService.UpdateDonation(Amount.Text, card.cardType, card.cardWpId, _campaign.CampaignWpId, null);
					if (updateResult.Success == false)
					{
						await DisplayAlert("Error", $"Error: {updateResult.ErrorMessage}", "OK");
					}
					else
					{
						await DisplayAlert("Success", "Successfully Updated Donation", "OK");
						await Navigation.PopAsync();
					}
				}
				DabUserInteractionEvents.WaitStopped(o, new EventArgs());
			}
			else 
			{
				AmountWarning.IsVisible = true;
			}
		}

		async void OnCancel(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
			var deleteResult = await Service.DabService.DeleteDonation(_campaign.Id);
			if (deleteResult.Success == false)
			{
				await DisplayAlert("Error", $"Error: {deleteResult.ErrorMessage}", "OK");
			}
			else
			{
				await DisplayAlert("Success", "Successfully Added Donation", "OK");
				await Navigation.PopAsync();
			}
			DabUserInteractionEvents.WaitStopped(o, new EventArgs());
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
