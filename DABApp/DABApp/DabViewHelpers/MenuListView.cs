using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
	public class MenuListView: ListView
	{
		public MenuListView()
		{
			List<MenuItem> data = new MenuListData();
			ItemsSource = data;
			VerticalOptions = LayoutOptions.FillAndExpand;
			BackgroundColor = Color.Transparent;

			var cell = new DataTemplate(typeof(ImageCell));
			cell.SetBinding(TextCell.TextProperty, "Title");
			cell.SetBinding(ImageCell.ImageSourceProperty, "IconSource");

			ItemTemplate = cell;
			SelectedItem = data[0];
		}
	}
}
