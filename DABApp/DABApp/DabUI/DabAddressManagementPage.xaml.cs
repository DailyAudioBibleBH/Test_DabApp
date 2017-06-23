using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAddressManagementPage : DabBaseContentPage
	{
		public DabAddressManagementPage()
		{
			InitializeComponent();
		}

		void OnBilling(object o, EventArgs e) 
		{
		}

		void OnShipping(object o, EventArgs e) 
		{
			DisplayAlert("Shipping Functionality Absent", "This has not been implemented yet", "OK");
		}
	}
}
