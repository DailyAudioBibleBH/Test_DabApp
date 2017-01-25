using System;
using Xamarin.Forms;

namespace DABApp
{
	public class DabRootPage: MasterDetailPage
	{
		public DabRootPage()
		{
			var menuPage = new DabMenuPage();
			menuPage.Menu.ItemSelected += (sender, e) => NavigateTo(e.SelectedItem as MenuItem);
			Master = menuPage;
			Detail = new NavigationPage(new DabChannelsPage());
		}

		void NavigateTo(MenuItem menu)
		{
			Page displayPage = (Page)Activator.CreateInstance(menu.TargetType);

			Detail = new NavigationPage(displayPage);

			IsPresented = false;
		}
	}
}
