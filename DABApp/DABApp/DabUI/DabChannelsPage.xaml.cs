using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;

namespace DABApp
{
	public partial class DabChannelsPage : DabBaseContentPage
	{
		View ChannelView;

		public DabChannelsPage()
		{
			InitializeComponent();

			//Choose a different control template to disable built in scroll view
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			this.ControlTemplate = playerBarTemplate;


			DabViewHelper.InitDabForm(this);
			ChannelView = ContentConfig.Instance.views.Single(x => x.id == 56);
			BindingContext = ChannelView;
			bannerContent.Text = ChannelView.banner.content;
			if (Device.Idiom == TargetIdiom.Phone)
			{
				banner.Source = ChannelView.banner.urlPhone;
			}
			else 
			{
				banner.Source = ChannelView.banner.urlTablet;
			}

			bannerContentContainer.SizeChanged += (object sender, EventArgs e) =>
			{
				//resize the banner image to match the banner content container's height
				banner.HeightRequest = bannerContentContainer.Height;
			};

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

		void OnChannel(object o, ItemTappedEventArgs e) {
			var resource = (Resource)e.Item;
		}
	}
}
