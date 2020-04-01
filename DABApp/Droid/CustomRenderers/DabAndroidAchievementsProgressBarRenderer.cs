using System;
using System.Runtime.Remoting.Contexts;
using DABApp;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(DabAchievementsProgressBar), typeof(DABApp.Droid.DabAndroidAchievementsProgressBarRenderer))]
namespace DABApp.Droid
{
    public class DabAndroidAchievementsProgressBarRenderer: ProgressBarRenderer
    {

        protected override void OnElementChanged(ElementChangedEventArgs<ProgressBar> e)
        {
            base.OnElementChanged(e);

            if (Element == null)
                return;

            var element = (DabAchievementsProgressBar)Element;

            if (Control != null)
            {
                Control.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.FromRgb(182, 231, 233).ToAndroid()); //Change the color
                Control.ScaleY = 10; //Changes the height

            }
        }
        
    }
}
