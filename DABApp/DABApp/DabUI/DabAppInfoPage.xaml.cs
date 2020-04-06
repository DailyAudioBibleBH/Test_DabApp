using System;
using System.Collections.Generic;
using Version.Plugin;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabAppInfoPage : DabBaseContentPage
	{
		public DabAppInfoPage()
		{
			InitializeComponent();
            if (GlobalResources.ShouldUseSplitScreen) { NavigationPage.SetHasNavigationBar(this, false); }
			BindingContext = ContentConfig.Instance.blocktext;
			VersionNumber.Text = $"Version Number {CrossVersion.Current.Version}";
		}
	}
}
