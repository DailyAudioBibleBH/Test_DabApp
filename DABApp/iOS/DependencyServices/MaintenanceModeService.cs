using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DABApp.Interfaces;
using DABApp.iOS;
using DABApp.iOS.DependencyServices;
using Foundation;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(MaintenanceModeService))]
namespace DABApp.iOS.DependencyServices
{
    public class MaintenanceModeService : IAppVersionName
    {
        public string GetVersionName()
        {
            return NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString").ToString();
        }
    }
}