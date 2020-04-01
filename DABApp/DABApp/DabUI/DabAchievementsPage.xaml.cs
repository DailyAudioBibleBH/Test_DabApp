﻿using DABApp.Helpers;
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
			string userName = GlobalResources.GetUserEmail();

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
			List<dbUserBadgeProgress> dbBadgeProgressList = db.Table<dbUserBadgeProgress>().ToList();

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
			var badgesWithProgress = dbBadgeList.Where(p => allBadgesQuery.All(p2 => p2.Progress.badgeId == p.id)).ToList();

			foreach (var item in badgesWithoutProgress)
            {
				dbUserBadgeProgress blankProgress = new dbUserBadgeProgress(item.id, userName);
				dabUserBadgeProgress newItem = new dabUserBadgeProgress(item, blankProgress);
				allBadges.Add(newItem);
            }


            //combined list of both badges with progress and badges with empty progress to bind to UI
			ObservableCollection<dabUserBadgeProgress> allAchievementsPageList = new ObservableCollection<dabUserBadgeProgress>(allBadges as List<dabUserBadgeProgress>);
			ObservableCollection<dabUserBadgeProgress> visibleAchievementsPageList = new ObservableCollection<dabUserBadgeProgress>();
			int currentYear = ContentConfig.Instance.options.progress_year;
            foreach (var item in allAchievementsPageList)
            {
				if (item.Progress.percent == 100)
				{
					item.Progress.opacity = 1;
				}
				else
				{
					item.Progress.opacity = .5;
				}
				if (item.Badge.visible == true && item.Progress.userName == userName && item.Progress.year == currentYear)
                {
					visibleAchievementsPageList.Add(item);
                }
            }
			foreach (var item in visibleAchievementsPageList)
			{
				item.Progress.percent = (float)item.Progress.percent / 100;
				item.Progress.progressColor = "Blue";
			}
			achievementListView.ItemsSource = visibleAchievementsPageList.OrderByDescending(x => x.Progress.percent).ToList();

			progressYear.Text = currentYear.ToString();

		}
	}
}