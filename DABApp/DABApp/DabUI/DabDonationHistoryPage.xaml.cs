using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabDonationHistoryPage : DabBaseContentPage
	{
		public DabDonationHistoryPage()
		{
			InitializeComponent();
		}

		void OnBack(object o, EventArgs e) 
		{
			Navigation.PopAsync();
		}
	}
}
