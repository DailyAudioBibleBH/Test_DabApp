using System;
using Xamarin.Forms;

namespace DABApp
{
	public class DabMenuPage: ContentPage
	{
		public ListView Menu { get; set;}

		public DabMenuPage()
		{
			Icon = "settings.png";
			Title = "Menu";
			BackgroundColor = Color.Red;

			Menu = new MenuListView();

			var menuLabel = new ContentView{ 
				Padding = new Thickness(10, 36, 0, 5),
				Content = new Label{
					TextColor = Color.White,
					Text="MENU"
				}
			};

			var layout = new StackLayout
			{
				Spacing = 0,
				VerticalOptions = LayoutOptions.FillAndExpand
			};
			layout.Children.Add(menuLabel);
			layout.Children.Add(Menu);

			Content = layout;
		}
	}
}
