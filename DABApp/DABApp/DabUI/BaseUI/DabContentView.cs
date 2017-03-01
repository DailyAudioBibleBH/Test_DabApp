using System;

using Xamarin.Forms;

namespace DABApp
{
	public class DabContentView : ContentPage
	{
		public DabContentView()
		{
			Content = new StackLayout
			{
				Children = {
					new Label { Text = "Hello ContentPage" }
				}
			};
		}
	}
}

