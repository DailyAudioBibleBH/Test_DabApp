using System;

using Xamarin.Forms;

namespace DABApp
{
	public class DrawerMenu : Grid
	{
		public DrawerMenu()
		{
			WidthRequest = 300;
				BackgroundColor = Color.Red;
				TranslationX = 400;
				HorizontalOptions = LayoutOptions.End;
				ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25, GridUnitType.Absolute) });
				ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				RowDefinitions.Add(new RowDefinition { Height = new GridLength(25, GridUnitType.Absolute) });
				RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
				RowSpacing = 25;

				var content = new StackLayout();
				var listView = new MenuListView();
				content.Children.Add(listView);
				Children.Add(content, 1, 1);

				MessagingCenter.Subscribe<DabChannelsPage>(this, "DrawerMenu", async (sender) =>
				{
					if (this.TranslationX == Application.Current.MainPage.Width)
					{
						await this.TranslateTo(0, 0, 250, Easing.Linear);
					}
					else {
						await this.TranslateTo(Application.Current.MainPage.Width, 0, 250, Easing.Linear);
					}
				});
		}
	}
}

