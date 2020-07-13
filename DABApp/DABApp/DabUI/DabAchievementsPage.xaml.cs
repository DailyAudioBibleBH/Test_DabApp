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
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			NavigationPage.SetHasBackButton(this, true);
			//Init the form
			DabViewHelper.InitDabForm(this);
			AchievementsView = contentView; 
			BindingContext = AchievementsView;
			string userName = GlobalResources.GetUserEmail();

			banner.Source = new UriImageSource
			{
				Uri = new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};

			//Connection to db
			SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


			int currentYear = ContentConfig.Instance.options.progress_year;
			//currentYear = 2020;

			//separate badge and progress list from db
			List<dbBadges> dbBadgeList = adb.Table<dbBadges>().Where(x => x.visible == true).ToListAsync().Result;
			List<dbUserBadgeProgress> dbBadgeProgressList = adb.Table<dbUserBadgeProgress>().Where(x => x.year == currentYear).ToListAsync().Result;

			//find badges that have progress
			IEnumerable<dabUserBadgeProgress> allBadgesQuery =
			from badge in dbBadgeList
			let badgeid = badge.id
			from progress in dbBadgeProgressList
			let progressbadgeid = progress.badgeId
			let currentUserName = progress.userName
			where badgeid == progressbadgeid && currentUserName == userName
			select new dabUserBadgeProgress(badge, progress)
			{
				Badge = badge,
				Progress = progress
			};

			var allBadges = allBadgesQuery.ToList();
			var badgesWithoutProgress = dbBadgeList.Where(p => allBadgesQuery.All(p2 => p2.Progress.badgeId != p.id)).ToList();

			foreach (var item in badgesWithoutProgress)
			{
				dbUserBadgeProgress blankProgress = new dbUserBadgeProgress(item.id, userName);
				dabUserBadgeProgress newItem = new dabUserBadgeProgress(item, blankProgress);

				allBadges.Add(newItem);
			}

			//combined list of both badges with progress and badges with empty progress to bind to UI
			ObservableCollection<dabUserBadgeProgress> allAchievementsPageList = new ObservableCollection<dabUserBadgeProgress>(allBadges as List<dabUserBadgeProgress>);
			ObservableCollection<dabUserBadgeProgress> visibleAchievementsPageList = new ObservableCollection<dabUserBadgeProgress>();

			foreach (var item in allAchievementsPageList)
			{
				if (item.Progress.percent == 100)
				{
					item.Progress.opacity = 1;
				}
				else
				{
					item.Progress.opacity = .4;
				}
				if (item.Badge.visible == true && item.Progress.userName == userName)
				{
					visibleAchievementsPageList.Add(item);
				}
			}



			//int i = 0;
			foreach (var item in visibleAchievementsPageList)
            {
				//i++;

				item.Progress.percent = (float)item.Progress.percent / 100;
				if (item.Progress.percent == 1 || item.Progress.percent == 0)
                {
					item.Progress.progressBarVisible = false;
                }
                else
                {
					item.Progress.progressBarVisible = true;
                }

//#if DEBUG
//				int r;
//				Math.DivRem(i, 2, out r);
//				if (r == 0)
//				{
//					item.Badge.imageURL = $"https://via.placeholder.com/400/ffffff/555555?text={item.Badge.badgeId}"; //testing image capture
//				}
//#endif
			}

			achievementListView.ItemsSource = visibleAchievementsPageList.OrderBy(x => x.Badge.id).ToList();
			achievementListView.HeightRequest = visibleAchievementsPageList.Count() * 200; //arbitrary number to get them tall enopugh.
			progressYear.Text = currentYear.ToString();
		}
	}
}