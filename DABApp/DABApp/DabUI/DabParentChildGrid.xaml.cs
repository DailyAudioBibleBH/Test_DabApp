using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabParentChildGrid : DabBaseContentPage
	{
		DABApp.View ContentView;

		public DabParentChildGrid(DABApp.View contentView)
		{
			InitializeComponent();
			BindingContext = contentView;
			ContentView = contentView;
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			ControlTemplate = playerBarTemplate;
			banner.Source = new UriImageSource
			{
				Uri =  new Uri((Device.Idiom == TargetIdiom.Phone? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};
		}

		void OnChildTapped(object o, ItemTappedEventArgs e)
		{
			var item = (View)e.Item;
			Content.BindingContext = item;
			ContentContainer.IsVisible = true;
		}
	}
}
