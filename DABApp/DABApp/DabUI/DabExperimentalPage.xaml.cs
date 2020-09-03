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
				case "LightMode":
					SecondIcon.IsVisible = true;
					break;
				case "DarkMode":
					ThirdIcon.IsVisible = true;
					break;
				case "System":
					FirstIcon.IsVisible = true;
					break;
			}
		}

		void OnModePicked(object o, EventArgs e)
		{
			var item = (ViewCell)o;
			switch (item.AutomationId)
			{
				case "SystemMode":
					dbSettings.StoreSetting("Display", "SystemMode");
					ExperimentalModeSettings.Instance.Display = "System";
					FirstIcon.IsVisible = true;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = false;
					break;
				case "LightMode":
					dbSettings.StoreSetting("Display", "LightMode");
					ExperimentalModeSettings.Instance.Display = "LightMode";
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = true;
					ThirdIcon.IsVisible = false;
					break;
				case "DarkMode":
					dbSettings.StoreSetting("Display", "DarkMode");
					ExperimentalModeSettings.Instance.Display = "DarkMode";
					FirstIcon.IsVisible = false;
					SecondIcon.IsVisible = false;
					ThirdIcon.IsVisible = true;
					break;
			}
			GlobalResources.SetDisplay();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
		}
	}
}
