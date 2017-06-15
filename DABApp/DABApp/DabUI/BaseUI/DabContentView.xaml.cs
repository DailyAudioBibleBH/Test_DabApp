using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabContentView : DabBaseContentPage
	{
		public DabContentView(DABApp.View contentView)
		{
			InitializeComponent();
			this.BindingContext = contentView;
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				bannerContainer.HeightRequest = 180;
			}

			if (contentView.title == "Initiatives")
			{
				if (Device.Idiom == TargetIdiom.Tablet)
				{
					Links.RowHeight = 300;
				}
				else
				{
					Links.RowHeight = 150;
				}
			}
			else {
				Links.RowHeight = 0;
			}
			if (contentView.children == null)
			{
				Children.IsVisible = false;
			}
			else {
				var length = 25 * contentView.children.Count;
				Children.HeightRequest = length;
			}
			if (string.IsNullOrEmpty(contentView.content)) {
				Content.IsVisible = false;
				ContentContainer.IsVisible = false;
			}
			if (contentView.links == null)
			{
				Links.IsVisible = false;
			}
			else { 
				ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
				this.ControlTemplate = playerBarTemplate;
			}

			banner.Source = new UriImageSource
			{
				Uri =  new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};
			//BannerTitle.Text = $"<h1 style=\"font-size:28px\">{contentView.title}</h1>";
		}

		void OnChildTapped(object o, ItemTappedEventArgs e) {
			var item = (View)e.Item;
			Navigation.PushAsync(new DabContentView(item));
			Children.SelectedItem = null;
		}

		void OnLinkTapped(object o, ItemTappedEventArgs e) {
			var item = (Link)e.Item;
			Navigation.PushAsync(new DabBrowserPage(item.link));
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			Links.SelectedItem = null;
		}
	}
}
