using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAboutPage : DabBaseContentPage
	{
		public DabAboutPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
		}
	}
}
