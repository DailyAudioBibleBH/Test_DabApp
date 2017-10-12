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
using Xamarin.Forms;
using SQLite;
using Xamarin.Forms.Platform.Android;
using Android.Graphics;
using Plugin.MediaManager;
using Plugin.MediaManager.MediaSession;
using Plugin.MediaManager.ExoPlayer;
using Android.Support.V4.Media.Session;
using FFImageLoading.Forms.Droid;
using HockeyApp.Android;
using HockeyApp.Android.Metrics;

namespace DABApp.Droid
{


	[Activity(Label = "DABApp.Droid", Icon = "@drawable/app_icon", Theme = "@style/MyTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
	[IntentFilter(new[] { Android.Content.Intent.ActionView }, DataScheme = "dab", Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable})]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle bundle)
		{
			SQLitePCL.Batteries.Init();
			SQLitePCL.raw.sqlite3_shutdown();
			SQLitePCL.raw.sqlite3_config(Convert.ToInt32(SQLite3.ConfigOption.Serialized));
			SQLitePCL.raw.sqlite3_enable_shared_cache(1);
			SQLitePCL.raw.sqlite3_initialize();

			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			CachedImageRenderer.Init();

			base.OnCreate(bundle);

			global::Xamarin.Forms.Forms.Init(this, bundle);
			DependencyService.Register<SocketService>();
			DependencyService.Register<FileManagement>();
			DependencyService.Register<StripeApiManagement>();
			DependencyService.Register<RivetsService>();

			SegmentedControlRenderer.Init();

			//CrossPushNotification.Initialize<CrossPushNotificationListener>("494133786726");

			SQLite_Droid.Assets = this.Assets;

			LoadApplication(new App());

			((MediaManagerImplementation)CrossMediaManager.Current).MediaSessionManager = new MediaSessionManager(Forms.Context, typeof(ExoPlayerAudioService));
			var exoPlayer = new ExoPlayerAudioImplementation(((MediaManagerImplementation)CrossMediaManager.Current).MediaSessionManager);
			CrossMediaManager.Current.AudioPlayer = exoPlayer;

			LoadCustomToolBar();
			MessagingCenter.Subscribe<string>("Setup", "Setup", (obj) => { LoadCustomToolBar(); });

		}

		protected override void OnResume()
		{
			base.OnResume();
			CrashManager.Register(this, "63fbcb2c3fcd4491b6c380f75d2e0d4d");
		}

		void LoadCustomToolBar()
		{
			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);
			var newMenu = LayoutInflater.Inflate(Resource.Layout.DabToolbar, null);
			var menu = (ImageButton)newMenu.FindViewById(Resource.Id.item1);
			menu.Click += (sender, e) => { MessagingCenter.Send<string>("Menu", "Menu"); };
			var give = (Android.Widget.Button)newMenu.FindViewById(Resource.Id.item2);
			give.SetTextColor(((Xamarin.Forms.Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid());
			give.Click += (sender, e) => { MessagingCenter.Send<string>("Give", "Give"); };
			var text = (TextView)newMenu.FindViewById(Resource.Id.textView1);
			text.SetTextColor(((Xamarin.Forms.Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid());
			text.Typeface = Typeface.CreateFromAsset(Assets, "FetteEngD.ttf");
			text.TextSize = 30;
			toolbar.AddView(newMenu);
			MessagingCenter.Subscribe<string>("Remove", "Remove", (obj) => { give.Visibility = ViewStates.Invisible;});
			MessagingCenter.Subscribe<string>("Show", "Show", (obj) => { give.Visibility = ViewStates.Visible; });
		}
	}
}
