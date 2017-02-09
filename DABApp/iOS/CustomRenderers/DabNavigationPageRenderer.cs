using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

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
			this.NavigationBar.TintColor = UIColor.White;
			this.NavigationBar.BarTintColor = UIColor.DarkGray;
			this.NavigationBar.BarStyle = UIBarStyle.BlackTranslucent;

		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);


		}

	}
}
