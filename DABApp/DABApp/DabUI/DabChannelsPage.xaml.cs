using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabChannelsPage : ContentPage
	{
		public DabChannelsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
		}

		void OnEpisodes(object o, EventArgs e) {
			Navigation.PushAsync(new DabEpisodesPage());
		}

		void OnMenu(object o, EventArgs e) {
			MessagingCenter.Send<DabChannelsPage>(this, "DrawerMenu");
		}
	}
}
