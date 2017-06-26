using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabWalletPage : DabBaseContentPage
	{
		public DabWalletPage(Card[] cards)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			foreach (var card in cards) {
				var image = new Image();
				image.Source = "ic_chevron_right_white_2x.png";
				image.HorizontalOptions = LayoutOptions.EndAndExpand;
				image.VerticalOptions = LayoutOptions.Center;
				var label = new Label();
				label.Text = $"{card.brand} **** **** **** {card.last4}";
				label.HorizontalOptions = LayoutOptions.StartAndExpand;
				label.VerticalOptions = LayoutOptions.Center;
				var stackLayout = new StackLayout();
				stackLayout.Orientation = StackOrientation.Horizontal;
				stackLayout.Children.Add(label);
				stackLayout.Children.Add(image);
				var viewCell = new ViewCell();
				viewCell.AutomationId = card.id;
				viewCell.Tapped += OnCard;
				viewCell.View = stackLayout;
				Cards.Add(viewCell);
			}
		}

		void OnCard(object o, EventArgs e) { 
			
		}
	}
}
