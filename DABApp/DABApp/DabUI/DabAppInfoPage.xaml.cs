﻿using System;
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

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            var adb = DabData.AsyncDatabase;
            //delete action dates
            var dateSettings = adb.Table<dbSettings>().Where(x => x.Key.StartsWith("ActionDate-")).ToListAsync().Result;
            foreach (var item in dateSettings)
            {
                int j = adb.DeleteAsync(item).Result;
            }

            //delete actions
            int i = adb.ExecuteAsync("delete from dbPlayerActions").Result;
            i = adb.ExecuteAsync("delete from dbEpisodeUserData").Result;


            DisplayAlert("Reset Local User Data", "We have reset your local user data. It will be reloaded when you return to the channels page.", "OK");
        }
    }
}
