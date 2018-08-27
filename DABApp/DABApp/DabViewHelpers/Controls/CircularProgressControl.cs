using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
    //Code found here: http://xamlnative.com/2016/04/14/xamarin-forms-a-simple-circular-progress-control/
    public class CircularProgressControl : Grid
    {
        Xamarin.Forms.View progress1;
        Xamarin.Forms.View progress2;
        Xamarin.Forms.View background1;
        Xamarin.Forms.View background2;
        Xamarin.Forms.View cloud1;
        Xamarin.Forms.View cloud2;
        bool reset= false;
        public CircularProgressControl()
        {
            IsVisible = false;
            progress1 = CreateImage("progress_done");
            background1 = CreateImage("progress_pending");
            background2 = CreateImage("progress_pending");
            progress2 = CreateImage("progress_done");
            cloud1 = CreateImage("ic_cloud_download_white");
            cloud2 = CreateImage("cloud_teal");
            cloud1.Opacity = .5;
            //cloud2.Opacity = .5;
            cloud2.IsVisible = false;
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(15) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Children.Add(progress1);
            Children.Add(background1);
            Children.Add(background2);
            Children.Add(progress2);
            SetColumnSpan(progress1, 3);
            SetColumnSpan(progress2, 3);
            SetColumnSpan(background1, 3);
            SetColumnSpan(background2, 3);
            SetRowSpan(progress1, 3);
            SetRowSpan(progress2, 3);
            SetRowSpan(background1, 3);
            SetRowSpan(background2, 3);
            Children.Add(cloud1, 1, 1);
            Children.Add(cloud2, 1, 1);
            if (Device.RuntimePlatform == "iOS" && Device.Idiom == TargetIdiom.Phone)
            {
                Margin = new Thickness(0, 0, 0, 10);
            }
            HandleProgressChanged(1, 0);
        }

        private Xamarin.Forms.View CreateImage(string v1)
        {
            var img = new Image();
            img.Source = ImageSource.FromFile(v1 + ".png");
            return img;
        }

        public static BindableProperty DownloadVisibleProperty = BindableProperty.Create("DownloadVisible", typeof(bool), typeof(CircularProgressControl), false, propertyChanged: DownloadVisibleChanged);

        public static BindableProperty ProgressProperty =
    BindableProperty.Create("Progress", typeof(double), typeof(CircularProgressControl), 0d, propertyChanged: ProgressChanged);

        public static BindableProperty ProgressVisibleProperty = BindableProperty.Create("ProgressVisible", typeof(bool), typeof(CircularProgressControl), false, propertyChanged: ProgressVisibleChanged);

        private static void ProgressChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var c = bindable as CircularProgressControl;
            c.HandleProgressChanged(Clamp((double)oldValue, 0, 1), Clamp((double)newValue, 0, 1));
        }

        private static void DownloadVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var c = bindable as CircularProgressControl;
            c.HandleDownloadVisibleChanged((bool)newValue);
        }

        private static void ProgressVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var c = (CircularProgressControl)bindable;
            c.IsVisible = (bool)newValue;
            if ((bool)newValue)
            {
                c.HandleProgressChanged(1, 0);
            }
        }

        static double Clamp(double value, double min, double max)
        {
            if (value <= max && value >= min) return value;
            else if (value > max) return max;
            else return min;
        }

        private void HandleProgressChanged(double oldValue, double p)
        {
            reset = false;
            if (p < .5 && !DownloadVisible)
            {
                if (oldValue >= .5)
                {
                    // this code is CPU intensive so only do it if we go from >=50% to <50%
                    background1.IsVisible = true;
                    progress2.IsVisible = false;
                    background2.Rotation = 180;
                    progress1.Rotation = 0;
                }
                double rotation = 360 * p;
                background1.Rotation = rotation;
            }
            else
            { 
                if (oldValue < .5)
                {
                    // this code is CPU intensive so only do it if we go from <50% to >=50%
                    background1.IsVisible = false;
                    progress2.IsVisible = true;
                    progress1.Rotation = 180;
                }
                double rotation = 360 * p;
                background2.Rotation = rotation;
            }
        }

        private void HandleDownloadVisibleChanged(bool newValue)
        {
            reset = true;
            progress1.IsVisible = !newValue;
            progress2.IsVisible = !newValue;
            background1.IsVisible = !newValue;
            background2.IsVisible = !newValue;
            cloud1.IsVisible = !newValue;
            cloud2.IsVisible = newValue;
        }

        public double Progress
        {
            get { return (double)this.GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public bool DownloadVisible
        {
            get { return (bool)this.GetValue(DownloadVisibleProperty); }
            set { SetValue(DownloadVisibleProperty, value); }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == "IsVisible" && IsVisible && reset)
            {
                HandleProgressChanged(1, 0);
            }
            base.OnPropertyChanged(propertyName);
        }
    }
}
