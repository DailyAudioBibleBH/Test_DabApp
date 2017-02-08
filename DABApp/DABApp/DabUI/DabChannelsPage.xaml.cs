using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabChannelsPage : MenuContainerPage
	{
		public DabChannelsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
		}

		void OnEpisodes(object o, EventArgs e) {
			Navigation.PushAsync(new DabEpisodesPage());
		}

		void OnPlayer(object o, EventArgs e) {
			ShowMenu();
		}

		void OnTest(object o, EventArgs e)
		{
			Navigation.PushAsync(new DabTestPage());
		}

		protected override void OnDisappearing(){
			base.OnDisappearing();
			HideMenu();
		}

		void OnPodcast(object o, EventArgs e) {
			Navigation.PushAsync(new DabPlayerView());
		}
	}
}
