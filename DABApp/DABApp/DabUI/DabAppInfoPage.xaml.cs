using System;
using System.Collections.Generic;
using System.Text;
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

            //build stats
            var adb = DabData.AsyncDatabase;

            StringBuilder stats = new StringBuilder();


            stats.AppendLine("System Stats");
            stats.AppendLine($"Content API: {DateTime.Parse(ContentConfig.Instance.data.updated)}");
            stats.AppendLine($"Channels: {adb.Table<dbChannels>().CountAsync().Result}");
            stats.AppendLine($"Episodes: {adb.Table<dbEpisodes>().CountAsync().Result}");
            stats.AppendLine($"User Episode Data: {adb.Table<dbEpisodeUserData>().CountAsync().Result}");
            stats.AppendLine($"Last Action Date GMT: {GlobalResources.LastActionDate}");
            stats.AppendLine($"Badges: {adb.Table<dbBadges>().CountAsync().Result}");
            stats.AppendLine($"User Progress Data: {adb.Table<dbUserBadgeProgress>().CountAsync().Result}");
            lblStats.Text = stats.ToString();

        }

        async void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            var adb = DabData.AsyncDatabase;
            //delete action dates
            var dateSettings = adb.Table<dbSettings>().Where(x => x.Key.StartsWith("ActionDate-")).ToListAsync().Result;
            foreach (var item in dateSettings)
            {
                int j = await adb.DeleteAsync(item);
            }

            //delete actions
            int i = adb.ExecuteAsync("delete from dbPlayerActions").Result;
            i = adb.ExecuteAsync("delete from dbEpisodeUserData").Result;


            await DisplayAlert("Local User Data Reset", "We have reset your local user data. It will be reloaded when you return to the episodes page.", "OK");
        }
    }
}
