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
		public DabAchievementsPage(DABApp.View contentView)
		{
			InitializeComponent();
			NavigationPage.SetHasBackButton(this, true);

			banner.Source = new UriImageSource
			{
				Uri =  new Uri((Device.Idiom == TargetIdiom.Phone ? contentView.banner.urlPhone : contentView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};
		}
	}
}