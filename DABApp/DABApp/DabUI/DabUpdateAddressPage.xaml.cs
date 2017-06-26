using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabUpdateAddressPage : DabBaseContentPage
	{
		public DabUpdateAddressPage(Address address)
		{
			InitializeComponent();
			BindingContext = address;
			if (address.isShipping) {
				EmailAndPhone.IsVisible = false;
				Title.Text = "Shipping Address";
			}
		}

		void OnSave(object o, EventArgs e) { 
		
		}
	}
}
