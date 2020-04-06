using System;
using System.Runtime.Remoting.Contexts;
using CoreGraphics;
using DABApp;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(DabAchievementsProgressBar), typeof(DABApp.iOS.DabIosAchievementsProgressBarRenderer))]
namespace DABApp.iOS
{
    public class DabIosAchievementsProgressBarRenderer : ProgressBarRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<ProgressBar> e)
        {
            base.OnElementChanged(e);

            if (Element == null)
                return;

            var element = (DabAchievementsProgressBar)Element;

            if (Control != null)
            {
                Control.TintColor = ((Color)App.Current.Resources["AchievementsProgressColor"]).ToUIColor();

            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            this.Transform = CGAffineTransform.MakeScale(1f, 20f);
            this.ClipsToBounds = true;
            this.Layer.MasksToBounds = true;
        }

    }
}

