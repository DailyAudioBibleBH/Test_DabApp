using System;
using Xamarin.Forms;

namespace DABApp
{
	public static class DabViewHelper
	{
		public static void InitDabForm(ContentPage page)
		{
			page.Title = "DAILY AUDIO BIBLE";

			ContentView content = (ContentView)page.Content;
			content.ControlTemplate = new ControlTemplate(typeof(DrawerMenu));
		}


	}
}
