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
			MainPage = new NavigationPage(new DabChannelsPage())
			{
				BarTextColor = Color.White,
				BarBackgroundColor = Color.Black
			};
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
