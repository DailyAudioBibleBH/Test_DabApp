using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEditRecurringDonationPage : DabBaseContentPage
	{
		public DabEditRecurringDonationPage(Donation campaign, Card[] cards)
		{
			InitializeComponent();
			Title.Text = campaign.name;
			Cards.ItemsSource = cards;
			Cards.ItemDisplayBinding = new Binding("last4");
			if (campaign.pro != null) {
				Amount.Text = campaign.pro.amount.ToString();
				Next.Date = Convert.ToDateTime(campaign.pro.next);
				Cards.SelectedItem = cards.Single(x => x.id == campaign.pro.card_id);
				Status.Text = campaign.pro.status;
			}
		}
	}
}
