using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.Droid.DependencyServices;
using DABApp.Interfaces;
using Plugin.Connectivity;
using Plugin.CurrentActivity;

[assembly: Xamarin.Forms.Dependency(typeof(MaintenanceModeService))]
namespace DABApp.Droid.DependencyServices
{
    public class MaintenanceModeService : IAppVersionName 
    {
        public string GetVersionName()
        {
            //Grab android version name 
            var context = CrossCurrentActivity.Current.AppContext;
            var _appInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
            var name = _appInfo.VersionName;
            return name;
        }
    }
}