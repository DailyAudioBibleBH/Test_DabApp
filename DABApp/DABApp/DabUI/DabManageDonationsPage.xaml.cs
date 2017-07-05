using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabManageDonationsPage : DabBaseContentPage
	{
		Donation[] _donations;

		public DabManageDonationsPage(Donation[] donations)
		{
			InitializeComponent();
			_donations = donations;
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (donations != null)
			{
				foreach (var don in donations.Reverse())
				{
					StackLayout layout = new StackLayout();
					Label cTitle = new Label();
					cTitle.Text = $"{don.name}-${don.suggestedRecurringDonation}/month";
					cTitle.Style = (Style)App.Current.Resources["playerLabelStyle"];
					Label card = new Label();
					Label recurr = new Label();
					if (don.pro != null)
					{
						card.Text = $"Card: **** **** **** {don.pro.card_last_four}";
						card.FontSize = 14;
						card.VerticalOptions = LayoutOptions.End;
						recurr.Text = $"Recurrs: {don.pro.next}";
						recurr.FontSize = 14;
						recurr.VerticalOptions = LayoutOptions.Start;
					}
					Button button = new Button();
					button.Text = "Edit Monthly";
					button.Clicked += OnRecurring;
					button.AutomationId = don.id.ToString();
					layout.Children.Add(cTitle);
					layout.Children.Add(card);
					layout.Children.Add(recurr);
					layout.Children.Add(button);
					layout.BackgroundColor = (Color)App.Current.Resources["InputBackgroundColor"];
					layout.Padding = 10;
					layout.Spacing = 10;
					Container.Children.Insert(1, layout);
				}
			}
		}

		void OnHistory(object o, EventArgs e) 
		{
			Navigation.PushAsync(new DabDonationHistoryPage());
		}

		async void OnRecurring(object o, EventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			Button chosen = (Button)o;
			Card[] cards = await AuthenticationAPI.GetWallet();
			var campaign = _donations.Single(x => x.id.ToString() == chosen.AutomationId);
			await Navigation.PushAsync(new DabEditRecurringDonationPage(campaign, cards));
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}
	}
}
