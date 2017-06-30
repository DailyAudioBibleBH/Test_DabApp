using System;
using SlideOverKit;
using Xamarin.Forms;
using FFImageLoading.Forms;

namespace DABApp
{
	public class DabBaseContentPage : MenuContainerPage
	{
		//public ActivityIndicator activity { get; set;}
		//public StackLayout activityHolder { get; set;}

		public DabBaseContentPage()
		{
			//Default Page properties
			//this.Padding = new Thickness(10, 10); //Add some padding around all page controls
			Title = "DAILY AUDIO BIBLE";
			//Control template (adds the player bar)
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplate"];
			RelativeLayout container = new RelativeLayout();
			ControlTemplate = playerBarTemplate;
			//activityHolder = new StackLayout()
			//
			//	Opacity = 0.5,
			//	BackgroundColor = Color.Gray,
			//	IsVisible = true
			//};
			//activity = new ActivityIndicator()
			//{
			//	IsRunning = true,
			//	IsVisible = true,
			//	VerticalOptions = LayoutOptions.CenterAndExpand,
			//	HorizontalOptions = LayoutOptions.CenterAndExpand,
			//	Color = Color.White
			//};
			//container.Children.Add(activityHolder, Constraint.RelativeToParent((parent) => { return parent.Width; }), Constraint.RelativeToParent((parent) => { return parent.Height; }));
			//container.Children.Add(activity, Constraint.RelativeToParent((parent) => { return parent.Width; }), Constraint.RelativeToParent((parent) => { return parent.Height; }));

			//ContentView view = new ContentView()
			//{
			//	Content = container,
			//	ControlTemplate = playerBarTemplate
			//};
			//Content = view;

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
			giveButton.Text = "Give";
			//giveButton.Icon = "ic_attach_money_white.png";
			giveButton.Priority = 0; //default
			giveButton.Clicked += async (sender, e) =>
			{
				await AuthenticationAPI.GetDonationAccessToken();
				await this.DisplayAlert("Give", "Thanks for giving!", "OK");
			};
			this.ToolbarItems.Add(giveButton);
		}

	}
}

