using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Color = Xamarin.Forms.Color;

//Attach this class to the NavigationPage renderer
[assembly: ExportRenderer(typeof(NavigationPage), typeof(DabNavigationPageRenderer))]

namespace DABApp.iOS
{
	public class DabNavigationPageRenderer : NavigationRenderer
	{
		public DabNavigationPageRenderer()
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			//Customize the navigation bar 
			this.NavigationBar.TintColor = ((Color)App.Current.Resources["TextColor"]).ToUIColor();
            if (GlobalResources.TestMode)
            {
                this.NavigationBar.BarTintColor = ((Color)App.Current.Resources["HighlightColor"]).ToUIColor();
            }
            else
            {
                this.NavigationBar.BarTintColor = ((Color)App.Current.Resources["NavBarBackgroundColor"]).ToUIColor();
            }
			this.NavigationBar.BarStyle = UIBarStyle.BlackTranslucent;
			this.NavigationBar.TitleTextAttributes = new UIStringAttributes()
			{
				Font = UIFont.FromName("FetteEngD", 25)
			};

		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);


		}

	}
}
