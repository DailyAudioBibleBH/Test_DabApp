using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DABApp.DabUI
{
    public partial class AchievementsProgressPopup
    {
        public AchievementsProgressPopup(DabSockets.DabGraphQlProgress progress)
        {
            InitializeComponent();

            //Connection to db
            SQLiteConnection db = DabData.database;
            SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

            List<dbBadges> currentBadges = db.Table<dbBadges>().Where(x => x.id == progress.badgeId).ToList();
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

            var breakpoint = "";
        }

        async void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent("achievement", "achievement");
        }

        async void OnContinue(object o, EventArgs e)
        {


        }
    }
}
