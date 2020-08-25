using DABApp.Helpers;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.XamarinForms.DataVisualization.Gauges;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DABApp
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DabAchievementsPage : DabBaseContentPage
	{
		View AchievementsView;
		Resource _resource;
		public int variable = 1;
		public DabAchievementsPage(DABApp.View contentView)
		{
			InitializeComponent();
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			NavigationPage.SetHasBackButton(this, true);
			//Init the form
			DabViewHelper.InitDabForm(this);
			AchievementsView = contentView;
			BindingContext = AchievementsView;
			string userName = dbSettings.GetSetting("Email", "");

			banner.Source = new UriImageSource
			{
				Uri = new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};

			//Connection to db
			SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


			int currentYear = ContentConfig.Instance.options.progress_year;  //TODO - replace with contentconfig for multi-year... ContentConfig.Instance.options.progress_year;

			//separate badge and progress list from db
			List<dbBadges> dbBadgeList = adb.Table<dbBadges>().ToListAsync().Result;
			List<dbUserBadgeProgress> dbBadgeProgressList = adb.Table<dbUserBadgeProgress>().ToListAsync().Result;

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

			//Summary Tab View
			dabUserBadgeProgress entireBibleBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.entire_bible_badge_id).ToList().SingleOrDefault();
			dabUserBadgeProgress oldTestamentBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.old_testament_badge_id).ToList().SingleOrDefault();
			dabUserBadgeProgress newTestamentBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.new_testament_badge_id).ToList().SingleOrDefault();

			EntireBibleGauge.Value = entireBibleBadge.Progress.percent;
			OldTestamentGauge.Value = oldTestamentBadge.Progress.percent;
			NewTestatmentGauge.Value = newTestamentBadge.Progress.percent;
			EntireBibleLabel.Text = entireBibleBadge.Progress.percent + "% Complete";
			OldTestamentLabel.Text = oldTestamentBadge.Progress.percent + "% Complete";
			NewTestamentLabel.Text = newTestamentBadge.Progress.percent + "% Complete";
			EntireBibleGradientOffset.Offset = entireBibleBadge.Progress.percent;
			OldTestamentGradientOffset.Offset = oldTestamentBadge.Progress.percent;
			NewTestamentGradientOffset.Offset = newTestamentBadge.Progress.percent;

			//Books Tab Collection View
			achievementListView.ItemsSource = visibleAchievementsPageList.Where(x => x.Badge.type == "books").OrderBy(x => x.Badge.id).ToList();
			achievementListView.HeightRequest = visibleAchievementsPageList.Count() * 200; //arbitrary number to get them tall enopugh.

			//Channels Tab Collection View
			channelsListView.ItemsSource = visibleAchievementsPageList.Where(x => x.Badge.type == "channels").OrderBy(x => x.Badge.id).ToList();
			channelsListView.HeightRequest = visibleAchievementsPageList.Count() * 200; //arbitrary number to get them tall enopugh.

			//Setting Progress Year picker
			List<string> yearList = makeYearList(currentYear);
			progressYear.SelectedItem = " " + currentYear.ToString() + " ∨";
			progressYear.ItemsSource = yearList;

            segmentControl.SelectionChanged += SegmentControl_SelectionChanged;
			segmentControl.SelectedIndex = 0;

			BooksTab.IsVisible = false;
			ChannelsTab.IsVisible = false;
			SummaryTab.IsVisible = true;

			var breakpoint = "";
		}

        private void SegmentControl_SelectionChanged(object sender, Telerik.XamarinForms.Common.ValueChangedEventArgs<int> e)
        {
            switch (e.NewValue)
            {
				case 0:
					Console.WriteLine("case 0");
					BooksTab.IsVisible = false;
					ChannelsTab.IsVisible = false;
					SummaryTab.IsVisible = true;
					break;
				case 1:
					Console.WriteLine("case 1");
					ChannelsTab.IsVisible = false;
					SummaryTab.IsVisible = false;
					BooksTab.IsVisible = true;
					break;
				case 2:
					Console.WriteLine("case 2");
					BooksTab.IsVisible = false;
					SummaryTab.IsVisible = false;
					ChannelsTab.IsVisible = true;
					break;
                default:
                    break;
            }
        }

        public List<string> makeYearList(int currentYear)
        {
			List<string> yearList = new List<string>();
			yearList.Add(currentYear.ToString());

            for (int i = currentYear; i < DateTime.Now.Year; i++)
            {
				var nextYear = currentYear - variable;
				yearList.Add(nextYear.ToString());
				variable = variable + 1;
				i++;
            }
			return yearList;
		}
	}
}