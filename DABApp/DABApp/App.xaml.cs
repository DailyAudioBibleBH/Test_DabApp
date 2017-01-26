using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
			ControlTemplate drawer = new ControlTemplate(typeof(DrawerMenu));
			MainPage = new NavigationPage(new DabChannelsPage())
			{
				BarTextColor = Color.White,
				BarBackgroundColor = Color.Black
			};
			//MessagingCenter.Subscribe<DabChannelsPage>(this, "DrawerMenu", async (sender) =>
			//{
			//	if (slideOver.TranslationX == Application.Current.MainPage.Width)
			//	{
			//		await slideOver.TranslateTo(0, 0, 250, Easing.Linear);
			//	}
			//	else{
			//		await slideOver.TranslateTo(Application.Current.MainPage.Width, 0, 250, Easing.Linear);
			//	}
			//});
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
