﻿using System;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public class DabBaseContentPage : MenuContainerPage
	{
		public DabBaseContentPage()
		{

			//Default Page properties
			this.Title = "Daily Audio Bible";

			//Control template (adds the player bar)
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplate"];
			this.ControlTemplate = playerBarTemplate;

			//Navigation properties
			NavigationPage.SetBackButtonTitle(this, "");

			//Slide Menu
			this.SlideMenu = new DabMenuView();

			//Menu Button
			var menuButton = new ToolbarItem();
			//menuButton.Text = "menu";
			menuButton.Priority = 1; //priority 1 causes it to be moved to the left by the platform specific navigation renderer
			menuButton.Icon = "ic_menu_white.png";
			menuButton.Clicked += (sender, e) =>
			{
				this.ShowMenu();
			};
			this.ToolbarItems.Add(menuButton);

			//Give button on the right (priority 1)
			var giveButton = new ToolbarItem();
			//giveButton.Text = "give";
			giveButton.Icon = "ic_attach_money_white.png";
			giveButton.Priority = 0; //default
			giveButton.Clicked += (sender, e) =>
				{
					this.DisplayAlert("Give", "Thanks for giving!", "OK");
				};
			this.ToolbarItems.Add(giveButton);

		}

	}
}
