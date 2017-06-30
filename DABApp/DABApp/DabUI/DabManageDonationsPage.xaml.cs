using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabManageDonationsPage : DabBaseContentPage
	{
		public DabManageDonationsPage(Donation[] donations)
		{
			InitializeComponent();
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
			if (donations != null)
			{
				foreach (var don in donations)
				{
					StackLayout layout = new StackLayout();
					Label label = new Label();
					label.Text = don.name;
					label.Style = (Style)App.Current.Resources["playerLabelStyle"];
					Button button = new Button();
					button.Text = "Edit Monthly";
					layout.Children.Add(label);
					layout.Children.Add(button);
					Container.Children.Add(layout);
				}
			}
		}

		void OnHistory(object o, EventArgs e) 
		{
			Navigation.PushAsync(new DabDonationHistoryPage());
		}
	}
}
