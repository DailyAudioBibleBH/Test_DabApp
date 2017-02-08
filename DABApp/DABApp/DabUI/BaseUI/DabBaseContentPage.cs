using System;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public class DabBaseContentPage : MenuContainerPage
	{
		public DabBaseContentPage()
		{
			//Content = new StackLayout
			//{
			//	Children = {
			//		new Label { Text = "This is a DabBaseContentPage. Your content should go here." }
			//	}
			//};

			//Default Page properties
			this.Title = "Daily Audio Bible";

			//Navigation
			NavigationPage.SetBackButtonTitle(this, "");

			//Slide Menu
			this.SlideMenu = new DabMenuView();
			//Slide Menu Button
			var menuButton = new ToolbarItem();
			menuButton.Text = "menu";
			//menuButton.Icon = "ic_menu_white.png";
			menuButton.Clicked += (sender, e) =>
			{
				this.ShowMenu();
			};
			this.ToolbarItems.Add(menuButton);
		}
	}
}

