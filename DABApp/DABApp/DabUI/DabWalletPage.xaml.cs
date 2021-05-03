using System;
using System.Collections.Generic;
using System.Linq;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabWalletPage : DabBaseContentPage
	{
		List<dbCreditCards> cards;
		object source;

		public DabWalletPage()
		{
			InitializeComponent();
			if (GlobalResources.ShouldUseSplitScreen)
            {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			cards = new List<dbCreditCards>();
			source = new object();
		}

		void OnCard(object o, EventArgs e) {
			var view = (ViewCell)o;
			Navigation.PushAsync(new DabCreditCardPage(cards.Single(x => x.cardWpId.ToString() == view.AutomationId)));
		}

		void OnAdd(object o, EventArgs e) {
			Navigation.PushAsync(new DabCreditCardPage());
		}

		protected override async void OnAppearing()
		{
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Loading cards...", true));
			Cards.Clear();
			InsertAddCardButton();
			cards.Clear();
			cards = DabServiceRoutines.GetWallet();
			foreach (var card in cards)
			{
				InsertCard(card);
			}
			base.OnAppearing();
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
		}

		void InsertCard(dbCreditCards card) 
		{ 
			var image = new Image();
			image.Source = "ic_chevron_right_white_2x.png";
			image.HorizontalOptions = LayoutOptions.EndAndExpand;
			image.VerticalOptions = LayoutOptions.Center;
			var label = new Label();

			string cardNumber;
			switch (card.cardType)
			{
				case "American Express":
					cardNumber = $"**** ****** *{card.cardLastFour}";
					break;
				default:
					cardNumber = $"**** **** **** {card.cardLastFour}";
					break;
			}

			try
            {
				label.Text = $"{card.cardType} {cardNumber} Expires {card.cardExpMonth}/{card.cardExpYear.ToString().Substring(2)}";
			}
			catch (Exception ex)
            {
				label.Text = $"{card.cardType} {cardNumber} Expires {card.cardExpMonth}/{card.cardExpYear}";
			}
			label.HorizontalOptions = LayoutOptions.StartAndExpand;
			label.VerticalOptions = LayoutOptions.Center;
            if (card.cardStatus == "deleted")
            {
				label.TextColor = Color.Gray;
            }
            else
            {
				label.TextColor = (Color)App.Current.Resources["PlayerLabelColor"];
			}
			var stackLayout = new StackLayout();
			stackLayout.Orientation = StackOrientation.Horizontal;
			stackLayout.Children.Add(label);
			stackLayout.Children.Add(image);
			stackLayout.Padding = 10;
			stackLayout.BackgroundColor = (Color)App.Current.Resources["InputBackgroundColor"];
			var viewCell = new ViewCell();
			viewCell.AutomationId = card.cardWpId.ToString();
			viewCell.Tapped += OnCard;
			viewCell.View = stackLayout;
			Cards.Insert(0, viewCell);
		}

		void InsertAddCardButton()
        {
			var button = new Button();
			button.Text = "Add a Card";
			button.Style = (Style)Application.Current.Resources["highlightedButtonStyle"];
			button.HeightRequest = 40;
            button.Clicked += OnAdd;


			var stackLayout = new StackLayout();
			stackLayout.Children.Add(button);
			var viewCell = new ViewCell();
			viewCell.Tapped += OnAdd;
			viewCell.View = stackLayout;
			Cards.Insert(0, viewCell);
		}
    }
}
