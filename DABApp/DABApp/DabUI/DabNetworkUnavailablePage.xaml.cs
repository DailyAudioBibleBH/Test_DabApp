using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabNetworkUnavailablePage : DabBaseContentPage
	{
		public DabNetworkUnavailablePage()
		{
			InitializeComponent();
		}

		void OnTryAgain(object o, EventArgs e) {
			TryAgain.IsEnabled = false;
			Message.IsVisible = false;
			if (ContentAPI.CheckContent())
			{
				var nav = new NavigationPage(new DabLoginPage());
				nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
				Navigation.PushModalAsync(nav);
			}
			else Message.IsVisible = true;
			TryAgain.IsEnabled = true;
		}
	}
}
