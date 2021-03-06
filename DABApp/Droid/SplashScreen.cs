using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using Android.OS;
using Android.Support.V7.App;
using Android.Content.PM;

namespace DABApp.Droid
{
	[Activity(Label = "Daily Audio Bible", Icon = "@drawable/app_icon", Theme = "@style/Theme.Splash", MainLauncher = true, NoHistory = true, ScreenOrientation=ScreenOrientation.Portrait)]
	public class SplashScreen: AppCompatActivity
	{
		static readonly string TAG = "X:" + typeof(SplashScreen).Name;

		public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
		{
			base.OnCreate(savedInstanceState, persistentState);
			Log.Debug(TAG, "SplashActivity.OnCreate");
		}

		protected override void OnResume()
		{
			base.OnResume();

			Task startupWork = new Task(() =>
			{
				Log.Debug(TAG, "Performing some startup work that takes a bit of time.");
				Task.Delay(5000);  // Simulate a bit of startup work.
				Log.Debug(TAG, "Working in the background - important stuff.");
			});

			startupWork.ContinueWith(t =>
			{
				Log.Debug(TAG, "Work is finished - start MainActivity.");
				StartActivity(new Intent(Application.Context, typeof(MainActivity)));
			}, TaskScheduler.FromCurrentSynchronizationContext());

			startupWork.Start();
		}
	}
}
