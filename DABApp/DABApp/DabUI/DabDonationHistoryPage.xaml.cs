﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabDonationHistoryPage : DabBaseContentPage
	{
		public DabDonationHistoryPage(DonationRecord[] history)
		{
			InitializeComponent();
			base.ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			History.ItemsSource = history;
			//foreach (var don in history.Reverse())
			//{
			//	StackLayout layout = new StackLayout();
			//	layout.Padding = 10;
			//	layout.BackgroundColor = (Color)App.Current.Resources["InputBackgroundColor"];
			//	Label cTitle = new Label();
			//	//Label cNumber = new Label();
			//	Label date = new Label();
			//	cTitle.Text = $"{don.campaignName}-{don.currency}{don.grossAmount}";
			//	cTitle.Style = (Style)App.Current.Resources["playerLabelStyle"];
			//	date.Text = don.date;
			//	layout.Children.Add(cTitle);
			//	//layout.Children.Add(cNumber);
			//	layout.Children.Add(date);
			//	Container.Children.Insert(1, layout);
			//}
		}

		void OnBack(object o, EventArgs e) 
		{
			Navigation.PopAsync();
		}
	}
}
