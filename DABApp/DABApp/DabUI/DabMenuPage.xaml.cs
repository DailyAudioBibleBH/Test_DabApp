using System;
using System.Collections.Generic;
using SlideOverKit;
using System.Linq;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabMenuPage : SlideMenuView
	{
		//public ListView ListView { get { return listView;} }

		public DabMenuPage()
		{
			InitializeComponent();
			this.IsFullScreen = true;
			this.WidthRequest = 250;
			this.MenuOrientations = MenuOrientation.RightToLeft;
			//listView.ItemsSource = new MenuListData();
		}

		void OnChannels(object o, EventArgs e) {
			Navigation.PopToRootAsync();
		}

		void OnAbout(object o, EventArgs e) {
			var existingPages = Navigation.NavigationStack.ToList();
			foreach (var page in existingPages) {
				if (page != existingPages[0])
				{
					Navigation.RemovePage(page);
				}
			}
			Navigation.PushAsync(new DabAboutPage());
		}

		//void OnBase(object o, ItemTappedEventArgs e) {
		//	var item = (MenuItem)e.Item;
		//	switch (item.Title) {
		//		case "Channels":
		//			Navigation.PopToRootAsync();
		//			break;
		//		case "About":
		//			Navigation.PushAsync(new DabAboutPage());
		//			break;
		//	}
		//}
	}
}
