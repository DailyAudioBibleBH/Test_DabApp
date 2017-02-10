using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabChannelsPage : DabBaseContentPage
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
			Navigation.PushAsync(new DabPlayerPage());
		}

		void OnTest(object o, EventArgs e)
		{
			Navigation.PushAsync(new DabTestContentPage());
		}

		protected override void OnDisappearing(){
			base.OnDisappearing();
			HideMenu();
		}

		void OnBrowse(object o, EventArgs e) {
			Navigation.PushAsync(new DabBrowserPage());
		}
	}
}
