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
        List<dbBadges> dbBadgeList;
        List<dbUserBadgeProgress> dbBadgeProgressList;
        string userName;
		int progressDuration;


		public DabAchievementsPage(DABApp.View contentView)
		{
			InitializeComponent();
			ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			NavigationPage.SetHasBackButton(this, true);
			//Init the form
			DabViewHelper.InitDabForm(this);
			AchievementsView = contentView;
			BindingContext = AchievementsView;
			userName = dbSettings.GetSetting("Email", "");
			progressDuration = ContentConfig.Instance.options.new_progress_duration;

			banner.Source = new UriImageSource
			{
				Uri = new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};

			//Get years of badges they can query
			int minYear = ContentConfig.Instance.options.progress_year;  //The minimum year allowed by the content api
			List<int> yearList = makeYearList(minYear); //list of years the user is allowed to query
			int currentYear = yearList.First();

			//Setting Progress Year picker
			progressYear.ItemsSource = yearList;
			progressYear.SelectedItem = currentYear;

			segmentControl.SelectionChanged += SegmentControl_SelectionChanged;
			segmentControl.SelectedIndex = 0;

			BooksTab.IsVisible = false;
			ChannelsTab.IsVisible = false;
			SummaryTab.IsVisible = true;
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

        public List<int> makeYearList(int firstYear)
        {
			List<int> yearList = new List<int>();

            for (int i = DateTime.Now.Year; i >= firstYear; i--) //sort descending
            {
				yearList.Add(i);
            }
			return yearList;
		}

        void progressYear_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
			var year = int.Parse(progressYear.SelectedItem.ToString());
			BindProgressControls(year);
        }

		void BindProgressControls(int year)
        {
			//Connection to db
			SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors
			//separate badge and progress list from db
			dbBadgeList = adb.Table<dbBadges>().ToListAsync().Result;
            dbBadgeProgressList = adb.Table<dbUserBadgeProgress>().ToListAsync().Result;

            //find badges that have progress
            IEnumerable<dabUserBadgeProgress> allBadgesQuery =
			from badge in dbBadgeList
			from progress in dbBadgeProgressList
			where badge.id == progress.badgeId && progress.userName == userName && progress.year == year
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
					item.Badge.description = item.Badge.description.Replace("Listen","Listened");
					if (item.Progress.updatedAt.AddDays(progressDuration) >= DateTime.Now)
						item.Progress.showNewIndicator = true;
					else
						item.Progress.showNewIndicator = false;


					item.Progress.opacity = 1;
				}
				else
				{
					item.Progress.showNewIndicator = false;
					item.Progress.opacity = .35;
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

			}

			//Summary Tab View
			var bibleBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.entire_bible_badge_id);
			double entireBibleBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.entire_bible_badge_id ).Select(x => x.Progress.percent).ToList().SingleOrDefault();
			double oldTestamentBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.old_testament_badge_id ).Select(x => x.Progress.percent).ToList().SingleOrDefault();
			double newTestamentBadge = allAchievementsPageList.Where(x => x.Badge.badgeId == ContentConfig.Instance.options.new_testament_badge_id ).Select(x => x.Progress.percent).ToList().SingleOrDefault();
			//Value of 0 breaks gauge so change to .01 for now and have label say 0
			if (entireBibleBadge == 0)
			{
				entireBibleBadge = .01;
				EntireBibleLabel.Text = "0% Complete";
			}
			else
			{
				EntireBibleLabel.Text = entireBibleBadge + "% Complete";
			}
			if (oldTestamentBadge == 0)
			{
				oldTestamentBadge = .01;
				OldTestamentLabel.Text = "0% Complete";
			}
			else
			{
				OldTestamentLabel.Text = oldTestamentBadge + "% Complete";
			}
			if (newTestamentBadge == 0)
			{
				newTestamentBadge = .01;
				NewTestamentLabel.Text = "0 % Complete";
			}
			else
			{
				NewTestamentLabel.Text = newTestamentBadge + "% Complete";
			}
			EntireBibleGauge.Value = entireBibleBadge;
			OldTestamentGauge.Value = oldTestamentBadge;
			NewTestatmentGauge.Value = newTestamentBadge;
			EntireBibleGradientOffset.Offset = entireBibleBadge;
			OldTestamentGradientOffset.Offset = oldTestamentBadge;
			NewTestamentGradientOffset.Offset = newTestamentBadge;

			if (entireBibleBadge == 100)
			{
				EntireBibleImage.Source = "EntireBibleCompleteDark1.png";
				EntireBibleGauge.StartThickness = 0;
				EntireBibleGauge.EndThickness = 0;
			}
            else
            {
				EntireBibleImage.Source = "EntireBibleDark.png";
				EntireBibleGauge.StartThickness = 12;
				EntireBibleGauge.EndThickness = 12;
            }

			if (oldTestamentBadge == 100)
			{
				OldTestamentImage.Source = "OldTestamentCompleteDark1.png";
				OldTestamentGauge.StartThickness = 0;
				OldTestamentGauge.EndThickness = 0;
			}
            else
            {
				OldTestamentImage.Source = "OldandNewTestamentDark.png";
				OldTestamentGauge.StartThickness = 5;
				OldTestamentGauge.EndThickness = 5;
            }
			if (newTestamentBadge == 100)
			{
				NewTestamentImage.Source = "OldTestamentCompleteDark1.png";
				NewTestatmentGauge.StartThickness = 0;
				NewTestatmentGauge.EndThickness = 0;
			}
            else
            {
				NewTestamentImage.Source = "OldandNewTestamentDark.png";
				NewTestatmentGauge.StartThickness = 5;
				NewTestatmentGauge.EndThickness = 5;
            }

			//Books Tab Collection View
			achievementListView.ItemsSource = visibleAchievementsPageList.Where(x => x.Badge.type == "books").OrderBy(x => x.Badge.id).ToList();
			achievementListView.HeightRequest = visibleAchievementsPageList.Count() * 200; //arbitrary number to get them tall enopugh.

			//Channels Tab Collection View
			channelsListView.ItemsSource = visibleAchievementsPageList.Where(x => x.Badge.type == "channels" && x.Progress.percent > 0).OrderBy(x => x.Badge.id).ToList();
			channelsListView.HeightRequest = visibleAchievementsPageList.Count() * 200; //arbitrary number to get them tall enopugh.
		}
    }
}