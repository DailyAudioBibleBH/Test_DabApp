using System;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public static class DabViewHelper
	{
		public static void InitDabForm(MenuContainerPage page)
		{
			NavigationPage.SetBackButtonTitle(page, "");
			page.Title = "DAILY AUDIO BIBLE";
			page.SlideMenu = new DabMenuPage();
		}


	}
}
