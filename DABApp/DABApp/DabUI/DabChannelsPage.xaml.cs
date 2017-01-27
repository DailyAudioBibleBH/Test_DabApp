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

		protected override void OnDisappearing(){
			base.OnDisappearing();
			this.HideMenu();
		}
	}
}
