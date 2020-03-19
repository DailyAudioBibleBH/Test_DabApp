using SQLite;
using System;
using System.Collections.Generic;
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
			//_resource = AchievementsView.resources[0];

			banner.Source = new UriImageSource
			{
				Uri =  new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};

			SQLiteConnection db = DabData.database;
			SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors

			List<dbBadges> dbBadgeList = db.Table<dbBadges>().ToList();
			List<dbBadgeProgress> dbBadgeProgress = db.Table<dbBadgeProgress>().ToList();

			var breakpoint = "";
			//List<dbBadges> badgeList = new List<dbBadges>();

			//foreach (var item in dbbadgeList)
			//{
			//	badgeList.Add(item);
			//}
		}
	}
}