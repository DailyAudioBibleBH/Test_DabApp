using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabRootPage : MasterDetailPage
	{
		public DabRootPage()
		{
			InitializeComponent();

			dabMenuPage.ListView.ItemSelected += OnItemSelected;
		}

		void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			var item = e.SelectedItem as MenuItem;
			if (item != null)
			{
				Detail = new NavigationPage((Page)Activator.CreateInstance(item.TargetType)) { 
					BarTextColor=Color.White,
					BarBackgroundColor=Color.Black
				};
				dabMenuPage.ListView.SelectedItem = null;
				IsPresented = false;
			}
		}
	}
}
