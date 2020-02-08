using System;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(DabSeekBar), typeof(DabSeekBarRenderer))]
namespace DABApp.Droid
{
    public class DabSeekBarRenderer : SliderRenderer
    {

        public DabSeekBarRenderer(Context context) : base(context)
        {
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var element = (DabSeekBar)Element;
            if (Control != null)
            {
                var seekbar = Control;

                seekbar.StartTrackingTouch += (sender, args) =>
                {
                    element.TouchDownEvent(this, EventArgs.Empty);
                };

                seekbar.StopTrackingTouch += (sender, args) =>
                {
                    element.TouchUpEvent(this, EventArgs.Empty);
                };

                seekbar.ProgressChanged += delegate (object sender, SeekBar.ProgressChangedEventArgs args)
                {
                    if (args.FromUser)
                        element.Value = (element.Minimum + ((element.Maximum - element.Minimum) * (args.Progress) / 1000.0));
                };
            }
        }
    }
}