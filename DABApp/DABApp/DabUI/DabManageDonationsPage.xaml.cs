using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabManageDonationsPage : DabBaseContentPage
	{
		Donation[] _donations;
		bool isInitialized = false;
		bool _fromLogin;

		public DabManageDonationsPage(Donation[] donations, bool fromLogin = false)
		{
			InitializeComponent();
			_donations = donations;
			_fromLogin = fromLogin;
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (donations != null)
			{
				foreach (var don in donations.Reverse())
				{
					StackLayout layout = new StackLayout();
					StackLayout buttons = new StackLayout();
					buttons.Orientation = StackOrientation.Horizontal;
					Button monthly = new Button();
					Button once = new Button();
					Label cTitle = new Label();
					cTitle.Text = $"{don.name}-${don.suggestedRecurringDonation}/month";
					cTitle.Style = (Style)App.Current.Resources["playerLabelStyle"];
					Label card = new Label();
					Label recurr = new Label();
					if (don.pro != null)
					{
						cTitle.Text = $"{don.name}-${don.pro.amount}/month";
						card.Text = $"Card ending in {don.pro.card_last_four}";
						card.FontSize = 14;
						card.VerticalOptions = LayoutOptions.End;
						recurr.Text = $"Recurs: {don.pro.next}";
						recurr.FontSize = 14;
						recurr.VerticalOptions = LayoutOptions.Start;
						monthly.Text = "Edit Monthly";
						monthly.Clicked += OnRecurring;
						monthly.WidthRequest = 150;
						monthly.AutomationId = don.id.ToString();
						once.Text = "One-time gift";
						buttons.Children.Add(monthly);
					}
					else 
					{
						once.Text = "Give";
						once.HorizontalOptions = LayoutOptions.StartAndExpand;
					}
					once.WidthRequest = 150;
					once.Clicked += OnGive;
					buttons.Children.Add(once);
					layout.Children.Add(cTitle);
					layout.Children.Add(card);
					layout.Children.Add(recurr);
					layout.Children.Add(buttons);
					layout.BackgroundColor = (Color)App.Current.Resources["InputBackgroundColor"];
					layout.Padding = 10;
					layout.Spacing = 10;
					layout.AutomationId = don.id.ToString();
					Container.Children.Insert(1, layout);
				}
			}
		}

		async void OnHistory(object o, EventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			DonationRecord[] history = await AuthenticationAPI.GetDonationHistory();
			await Navigation.PushAsync(new DabDonationHistoryPage(history));
			isInitialized = false;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
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

		async void OnGive(object o, EventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var url = await PlayerFeedAPI.PostDonationAccessToken();
			DependencyService.Get<IRivets>().NavigateTo(url);
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			if (_fromLogin) {
				Navigation.InsertPageBefore(new DabChannelsPage(), this);
			}
			if (isInitialized)
			{
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				_donations = await AuthenticationAPI.GetDonations();
				foreach (var don in _donations)
				{
					StackLayout donContainer = (StackLayout)Container.Children.Single(x => x.AutomationId == don.id.ToString());
					var Labels = donContainer.Children.Where(x => x.GetType() == typeof(Label)).Select(x => (Label)x).ToList();
					if (don.pro != null)
					{
						Labels[0].Text = $"{don.name}-${don.pro.amount}/month";
						Labels[1].Text = $"Card ending in {don.pro.card_last_four}";
						Labels[2].Text = $"Recurs: {don.pro.next}";
						Labels[1].IsVisible = true;
						Labels[2].IsVisible = true;
					}
					else
					{
						Labels[1].Text = null;
						Labels[2].Text = null;
					}
				}
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
			}
			isInitialized = true;
		}
	}
}
