using DABApp.Helpers;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DABApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DabAchievementsPage : DabBaseContentPage
	{
		View AchievementsView;
		Resource _resource;
		public DabAchievementsPage(DABApp.View contentView)
		{
			InitializeComponent();
			NavigationPage.SetHasBackButton(this, true);
			//Init the form
			DabViewHelper.InitDabForm(this);
			AchievementsView = ContentConfig.Instance.views.Single(x => x.id == 132262); //TODO: Find this using a key vs. a specific number
			BindingContext = AchievementsView;

			banner.Source = new UriImageSource
			{
				Uri =  new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};

            //Connection to db
			SQLiteConnection db = DabData.database;
			SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

            //separate badge and progress list from db
			List<dbBadges> dbBadgeList = db.Table<dbBadges>().ToList();
			List<dbBadgeProgress> dbBadgeProgressList = db.Table<dbBadgeProgress>().ToList();

            //find badges that have progress
			IEnumerable<dabUserBadgeProgress> queryBadgesWithProgress =
			from badge in dbBadgeList
			let badgeid = badge.id
			from progress in dbBadgeProgressList
			let progressbadgeid = progress.badgeId
			where badgeid == progressbadgeid
			select new dabUserBadgeProgress(badge, progress)
			{
				Badge = badge,
				Progress = progress
			};

			var badgesWithProgress = queryBadgesWithProgress.ToList();
			var badgesWithoutProgress = dbBadgeList.Where(p => queryBadgesWithProgress.All(p2 => p2.Progress.badgeId != p.id)).ToList();

            foreach (var item in badgesWithoutProgress)
            {
				dbBadgeProgress blankProgress = new dbBadgeProgress();
				dabUserBadgeProgress newItem = new dabUserBadgeProgress(item, blankProgress);
				badgesWithProgress.Add(newItem);
            }

            //combined list of both badges with progress and badges with empty progress to bind to UI
			ObservableCollection<dabUserBadgeProgress> achievementsPageList = new ObservableCollection<dabUserBadgeProgress>(badgesWithProgress as List<dabUserBadgeProgress>);

			achievementListView.ItemsSource = achievementsPageList;
        }
	}
}