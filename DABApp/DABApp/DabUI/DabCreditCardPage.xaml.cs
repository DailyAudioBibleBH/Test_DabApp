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
			if (Device.Idiom == TargetIdiom.Tablet) {
				NavigationPage.SetHasNavigationBar(this, false);
			}
		}

		void OnSave(object o, EventArgs e) { 
		
		}

		void OnDelete(object o, EventArgs e) { }
	}
}
