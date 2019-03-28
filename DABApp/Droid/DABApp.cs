using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace DABApp.Droid
{
	[Application]
	public class DABApp: Application
	{
		public static Context AppContext;

		public DABApp(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    	{

		}

		public override void OnCreate()
		{
			base.OnCreate();

			AppContext = this.ApplicationContext;

			SQLite_Droid.Assets = this.Assets;

			//TODO: Initialize CrossPushNotification Plugin
			//TODO: Replace string parameter with your Android SENDER ID
			//TODO: Specify the listener class implementing IPushNotificationListener interface in the Initialize generic
			//CrossPushNotification.Initialize<CrossPushNotificationListener>("494133786726");

			//This service will keep your app receiving push even when closed.             
			//StartPushService();
        }

		//public static void StartPushService()
		//{
		//	AppContext.StartService(new Intent(AppContext, typeof(PushNotificationService)));

		//	if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
		//	{

		//		PendingIntent pintent = PendingIntent.GetService(AppContext, 0, new Intent(AppContext, typeof(PushNotificationService)), 0);
		//		AlarmManager alarm = (AlarmManager)AppContext.GetSystemService(Context.AlarmService);
		//		alarm.Cancel(pintent);
		//	}
		//}

		//public static void StopPushService()
		//{
		//	AppContext.StopService(new Intent(AppContext, typeof(PushNotificationService)));
		//	if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
		//	{
		//		PendingIntent pintent = PendingIntent.GetService(AppContext, 0, new Intent(AppContext, typeof(PushNotificationService)), 0);
		//		AlarmManager alarm = (AlarmManager)AppContext.GetSystemService(Context.AlarmService);
		//		alarm.Cancel(pintent);
		//	}
		//}
	}
}
