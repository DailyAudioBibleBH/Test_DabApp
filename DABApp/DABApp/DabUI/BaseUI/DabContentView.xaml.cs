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
			if (Device.Idiom == TargetIdiom.Phone)
			{
				banner.Source = contentView.banner.urlPhone;
			}
			else{
				banner.Source = contentView.banner.urlTablet;
			}
		}

		void OnChildTapped(object o, ItemTappedEventArgs e) {
			var item = (View)e.Item;
			Navigation.PushAsync(new DabContentView(item));
		}

		void OnLinkTapped(object o, ItemTappedEventArgs e) {
			var item = (Link)e.Item;
			Device.OpenUri(new Uri(item.link));
		}
	}
}
