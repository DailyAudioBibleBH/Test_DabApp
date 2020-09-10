using DABApp.DabUI.BaseUI;
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
		object source;

		public DabManageDonationsPage(Donation[] donations, bool fromLogin = false)
		{
			InitializeComponent();
			if (Device.RuntimePlatform == "Android")
			{
                if (Device.Idiom == TargetIdiom.Phone)
                {
                    MessagingCenter.Send<string>("Remove", "Remove");
                }
                //else { NavigationPage.SetHasNavigationBar(this, false); }
			}
			else 
			{ 
				ToolbarItems.RemoveAt(ToolbarItems.Count - 1);
			}
			_donations = donations;
			_fromLogin = fromLogin;
			if (GlobalResources.ShouldUseSplitScreen)
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			MessagingCenter.Subscribe<string>("Refresh", "Refresh", (sender) =>{
				OnAppearing();
			});
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
					monthly.Text = "Edit Monthly";
					monthly.Clicked += OnRecurring;
					monthly.WidthRequest = 150;
					monthly.AutomationId = don.id.ToString();
					if (don.pro != null)
					{
						cTitle.Text = $"{don.name}-${don.pro.amount}/month";
						card.Text = $"Card ending in {don.pro.card_last_four}";
						card.FontSize = 14;
						card.VerticalOptions = LayoutOptions.End;
						recurr.Text = $"Recurs: {don.pro.next}";
						recurr.FontSize = 14;
						recurr.VerticalOptions = LayoutOptions.Start;
						monthly.IsVisible = true;
						once.Text = "One-time gift";
						buttons.Children.Add(monthly);
					}
					else 
					{
						monthly.IsVisible = false;
						once.Text = "Give";
						once.HeightRequest = 40;
						once.HorizontalOptions = LayoutOptions.StartAndExpand;
					}
					once.WidthRequest = 150;
					once.AutomationId = don.id.ToString();
					once.Clicked += OnGive;
					buttons.Children.Add(monthly);
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
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
			DonationRecord[] history = await AuthenticationAPI.GetDonationHistory();
			if (history != null)
			{
				await Navigation.PushAsync(new DabDonationHistoryPage(history));
			}
			else
			{
				await DisplayAlert("Unable to retrieve Donation information", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
			}
			isInitialized = false;
			GlobalResources.WaitStop();
		}

		async void OnRecurring(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
			Button chosen = (Button)o;
			Card[] cards = await AuthenticationAPI.GetWallet();
			var campaign = _donations.Single(x => x.id.ToString() == chosen.AutomationId);
			await Navigation.PushAsync(new DabEditRecurringDonationPage(campaign, cards));
			GlobalResources.WaitStop();
		}

		async void OnGive(object o, EventArgs e) 
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
			Button chosen = (Button)o;
			var url = await PlayerFeedAPI.PostDonationAccessToken(chosen.AutomationId);
			if (!url.Contains("Error"))
			{
				DependencyService.Get<IRivets>().NavigateTo(url);
			}
			else 
			{
				await DisplayAlert("An Error has occured.", url, "OK");
			}
			GlobalResources.WaitStop();
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			if (_fromLogin) {
				Navigation.InsertPageBefore(new DabChannelsPage(), this);
			}
			if (Device.Idiom == TargetIdiom.Phone)
			{
				MessagingCenter.Send<string>("Remove", "Remove");
			}
			if (isInitialized)
			{
				source = new object();
				DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));

				_donations = await AuthenticationAPI.GetDonations();
				if (_donations != null)
				{
					foreach (var don in _donations)
					{
						StackLayout donContainer = (StackLayout)Container.Children.SingleOrDefault(x => x.AutomationId == don.id.ToString());
						var Labels = donContainer.Children.Where(x => x.GetType() == typeof(Label)).Select(x => (Label)x).ToList();
						var ButtonContainer = donContainer.Children.SingleOrDefault(x => x.GetType() == typeof(StackLayout)) as StackLayout;
						var Buttons = ButtonContainer.Children.Where(x => x.GetType() == typeof(Button)).Select(x => (Button)x).ToList();
						if (don.pro != null)
						{
							Labels[0].Text = $"{don.name}-${don.pro.amount}/month";
							Labels[1].Text = $"Card ending in {don.pro.card_last_four}";
							Labels[2].Text = $"Recurs: {don.pro.next}";
							Labels[1].IsVisible = true;
							Labels[2].IsVisible = true;
							Labels[1].FontSize = 14;
							Labels[2].FontSize = 14;
							Buttons[0].IsVisible = true;
							Buttons[1].Text = "One-time gift";
						}
						else
						{
							Labels[1].Text = null;
							Labels[2].Text = null;
							Buttons[0].IsVisible = false;
							Buttons[1].Text = "Give";
							Buttons[1].HeightRequest = 40;
						}
					}
				}
				else
				{
					await DisplayAlert("Unable to retrieve Donation information", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
					//await Navigation.PopAsync();
				}
				GlobalResources.WaitStop();
			}
			isInitialized = true;
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			MessagingCenter.Send<string>("Show", "Show");
		}
	}
}
