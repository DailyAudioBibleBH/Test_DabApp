using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabWalletPage : DabBaseContentPage
	{
		Card[] _cards;
		bool isInitialized = false;

		public DabWalletPage(Card[] cards)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			_cards = cards;
			foreach (var card in cards) {
				InsertCard(card);
			}
		}

		void OnCard(object o, EventArgs e) {
			var view = (ViewCell)o;
			Navigation.PushAsync(new DabCreditCardPage(_cards.Single(x => x.id == view.AutomationId)));
		}

		void OnAdd(object o, EventArgs e) {
			Navigation.PushAsync(new DabCreditCardPage());
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			if (isInitialized)
			{
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				var result = await AuthenticationAPI.GetWallet();
				if (result != null)
				{
					ViewCell ok = (ViewCell)Cards.Last();
					Cards.Clear();
					Cards.Add(ok);
					foreach (var cell in result)
					{
						InsertCard(cell);
					}
					_cards = result;
				}
				else 
				{
					await DisplayAlert("Unable to retrieve Wallet information.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
					await Navigation.PopAsync();
				}
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
			}
			isInitialized = true;
		}

		void InsertCard(Card card) 
		{ 
			var image = new Image();
			image.Source = "ic_chevron_right_white_2x.png";
			image.HorizontalOptions = LayoutOptions.EndAndExpand;
			image.VerticalOptions = LayoutOptions.Center;
			var label = new Label();
			label.Text = $"{card.brand} **** **** **** {card.last4}";
			label.HorizontalOptions = LayoutOptions.StartAndExpand;
			label.VerticalOptions = LayoutOptions.Center;
			label.TextColor = (Color)App.Current.Resources["PlayerLabelColor"];
			var stackLayout = new StackLayout();
			stackLayout.Orientation = StackOrientation.Horizontal;
			stackLayout.Children.Add(label);
			stackLayout.Children.Add(image);
			stackLayout.Padding = 10;
			stackLayout.BackgroundColor = (Color)App.Current.Resources["InputBackgroundColor"];
			var viewCell = new ViewCell();
			viewCell.AutomationId = card.id;
			viewCell.Tapped += OnCard;
			viewCell.View = stackLayout;
			Cards.Insert(0, viewCell);
		}
	}
}
