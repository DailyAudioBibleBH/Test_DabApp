using DABApp.DabSockets;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Services;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DABApp.DabUI
{
    public partial class AchievementsProgressPopup
    {
        dbBadges currentBadge;
        string badgeName;
        int progressId;
        public AchievementsProgressPopup(DabSockets.DabGraphQlProgress progress)
        {
            InitializeComponent();

            progressId = progress.id;

            //Connection to db
            SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

            List<dbBadges> currentBadges = adb.Table<dbBadges>().Where(x => x.id == progress.badgeId).ToListAsync().Result;
            dbBadges currentBadge = new dbBadges();

            foreach (var item in currentBadges)
            {
                if (item.id == progress.badgeId)
                {
                    currentBadge = item;
                }
            }

            AchievementImage.Source = currentBadge.imageURL;
            Title.Text = currentBadge.name;
            Description.Text = currentBadge.description;
            badgeName = currentBadge.name;

            try
            {
                //Send info to Firebase analytics that user achieveed the badge
                var infoJ = new Dictionary<string, string>();
                infoJ.Add("badge", currentBadge.name);
                infoJ.Add("user", GlobalResources.GetUserEmail());
                DependencyService.Get<IAnalyticsService>().LogEvent("badge_earned", infoJ);

            }
            catch (Exception ex)
            {

            }
        }

        public async Task ShareUri(string uri)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                //Uri = uri,
                Text = "I earned the " + badgeName + " Badge listening to the Daily Audio Bible! You can earn one too at https://player.dailyaudiobible.com!",
                Title = "I earned the " + badgeName + " Badge listening to the Daily Audio Bible! You can earn one too at https://player.dailyaudiobible.com!"
            });
        }

        async void OnShare(object o, EventArgs e)
        {
            await ShareUri("https://player.dailyaudiobible.com");
        }

        async void OnContinue(object o, EventArgs e)
        {
            //TODO: Fully implement this - it's not working yet and will crash the app
            await Service.DabService.SeeProgress(progressId);
            await PopupNavigation.Instance.PopAsync();
        }
    }
}
