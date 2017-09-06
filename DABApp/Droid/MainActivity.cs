using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using SegmentedControl.FormsPlugin.Android;
using PushNotification.Plugin;
using Android.Gms.Gcm.Iid;
using Android.Gms.Gcm;
using Android.Util;
using Android.Support.V7;
using FFImageLoading.Forms.Droid;
using Xamarin.Forms;
using SQLite;

namespace DABApp.Droid
{


	[Activity(Label = "DABApp.Droid", Icon = "@drawable/icon", Theme = "@style/MyTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle bundle)
		{
			SQLitePCL.Batteries.Init();
			SQLitePCL.raw.sqlite3_shutdown();
			SQLitePCL.raw.sqlite3_config(Convert.ToInt32(SQLite3.ConfigOption.Serialized));
			SQLitePCL.raw.sqlite3_initialize();

			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			CachedImageRenderer.Init();

			base.OnCreate(bundle);

			global::Xamarin.Forms.Forms.Init(this, bundle);
			DependencyService.Register<SocketService>();

			SegmentedControlRenderer.Init();

			//CrossPushNotification.Initialize<CrossPushNotificationListener>("494133786726");

			SQLite_Droid.Assets = this.Assets;

			LoadApplication(new App());
		}
	}
}
