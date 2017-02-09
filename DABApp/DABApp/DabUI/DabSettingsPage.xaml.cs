using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSettingsPage : DabBaseContentPage
	{
		public DabSettingsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
		}
	}
}
