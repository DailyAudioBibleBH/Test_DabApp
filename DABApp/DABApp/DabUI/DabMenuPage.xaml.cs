using System;
using System.Collections.Generic;
using SlideOverKit;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabMenuPage : SlideMenuView
	{
		public ListView ListView { get { return listView;} }

		public DabMenuPage()
		{
			InitializeComponent();
			this.IsFullScreen = true;
			this.WidthRequest = 250;
			this.MenuOrientations = MenuOrientation.RightToLeft;
			listView.ItemsSource = new MenuListData();
		}

		void OnBase(object o, ItemTappedEventArgs e) {
			var item = (MenuItem)e.Item;
			switch (item.Title) {
				case "Channels":
					Navigation.PopToRootAsync();
					break;
				case "About":
					Navigation.PopToRootAsync();
					Navigation.PushAsync(new DabAboutPage());
					break;
			}
		}
	}
}
