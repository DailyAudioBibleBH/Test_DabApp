using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabExperimentalPage : DabBaseContentPage
    {
        public DabExperimentalPage()
        {
			InitializeComponent();
			if (GlobalResources.ShouldUseSplitScreen) { NavigationPage.SetHasNavigationBar(this, false); }
			DabViewHelper.InitDabForm(this);
			switch (ExperimentalModeSettings.Instance.Display)
			{
				case "Light Mode":
					FirstIcon.IsVisible = true;
					break;
				case "Dark Mode":
					SecondIcon.IsVisible = true;
					break;
				case "System":
					ThirdIcon.IsVisible = true;
					break;
			}
		}

		void OnModePicked(object o, EventArgs e)
		{
			var item = (ViewCell)o;
			switch (item.AutomationId)
			{
				case "SystemMode":
					FirstIcon.IsVisible = true;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = false;
					ExperimentalModeSettings.Instance.Display = "System";
					break;
				case "LightMode":
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = true;
					ThirdIcon.IsVisible = false;
					ExperimentalModeSettings.Instance.Display = "LightMode";
					break;
				case "DarkMode":
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = true;
					ExperimentalModeSettings.Instance.Display = "DarkMode";
					break;
			}
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
		}
	}
}
