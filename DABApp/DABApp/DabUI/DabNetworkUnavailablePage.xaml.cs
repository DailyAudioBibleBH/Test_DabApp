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
				Navigation.PushModalAsync(new NavigationPage(new DabChannelsPage()));
			}
			else Message.IsVisible = true;
			TryAgain.IsEnabled = true;
		}
	}
}
