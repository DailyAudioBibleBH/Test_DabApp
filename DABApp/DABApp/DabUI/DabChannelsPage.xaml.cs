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
		}

		void OnEpisodes(object o, EventArgs e) {
			Navigation.PushAsync(new DabEpisodesPage());
		}
	}
}
