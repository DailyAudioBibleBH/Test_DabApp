using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabCreditCardPage : DabBaseContentPage
	{
		public DabCreditCardPage(Card card = null)
		{
			InitializeComponent();
			var months = new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"};
			Month.ItemsSource = months;
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (card != null) {
				Title.Text = "Card Details";
				CVC.IsVisible = false;
				CVCLabel.IsVisible = false;
				Delete.IsVisible = true;
				Save.IsVisible = false;
				DeleteText.IsVisible = true;
				CardNumber.IsEnabled = false;
				CardNumber.Text = $"**** **** **** {card.last4}";
				Month.IsEnabled = false;
				Month.SelectedItem = card.exp_month.ToString();
				Year.IsEnabled = false;
			}
		}

		void OnSave(object o, EventArgs e) { 
		
		}

		void OnDelete(object o, EventArgs e) { }
	}
}
