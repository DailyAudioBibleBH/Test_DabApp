using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabChannelsPage : DabBaseContentPage
	{
		View ChannelView;

		public DabChannelsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			ChannelView = ContentConfig.Instance.views.Single(x => x.id == 56);
			BindingContext = ChannelView;
			Header.Text = ChannelView.banner.content;
			if (Device.Idiom == TargetIdiom.Phone)
			{
				banner.Source = ChannelView.banner.urlPhone;
			}
			else 
			{
				banner.Source = ChannelView.banner.urlTablet;
			}
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
			Navigation.PushAsync(new DabBrowserPage("http://c2itconsulting.net/"));
		}
	}
}
