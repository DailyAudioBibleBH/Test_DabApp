using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabMenuPage : ContentPage
	{
		public ListView ListView { get { return listView;} }

		public DabMenuPage()
		{
			InitializeComponent();

			listView.ItemsSource = new MenuListData();
		}
	}
}
