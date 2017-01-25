using System;
using Xamarin.Forms;

namespace DABApp
{
	public class App : Application
	{
		public static MasterDetailPage masterDetailPage;
		
		public App()
		{
			MainPage = new DabRootPage();
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
