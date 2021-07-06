using System;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using DABApp.Droid.DependencyServices;

namespace DABApp.Droid
{
    [Application]
    public class DABApp : Application
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
        }
    }
}
