using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;
using FFImageLoading;

namespace DABApp
{
	public partial class DabChannelsPage : DabBaseContentPage
	{
		View ChannelView;
		dbEpisodes episode;

		public DabChannelsPage()
		{
			InitializeComponent();

			////Choose a different control template to disable built in scroll view
			//ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			//this.ControlTemplate = playerBarTemplate;

			DabViewHelper.InitDabForm(this);
			ChannelView = ContentConfig.Instance.views.Single(x => x.id == 56);
			BindingContext = ChannelView;
			bannerContent.Text = ChannelView.banner.content;
			var resource = ChannelView.resources[0];
			PlayerFeedAPI.GetEpisodes(resource);
			episode = PlayerFeedAPI.GetMostRecentEpisode(resource);
			if (episode == null)
			{
				bannerContentContainer.IsVisible = false;
			}
			else
			{
				bannerContentContainer.IsVisible = true;
				var oldText = bannerContent.Text;
				bannerContent.Text = oldText.Replace("[current_reading]", episode.description);
			}

			banner.Source = new UriImageSource
			{
				Uri = new Uri((Device.Idiom == TargetIdiom.Phone ? ChannelView.banner.urlPhone : ChannelView.banner.urlTablet)),
				CacheValidity = GlobalResources.ImageCacheValidity
			};

			bannerContentContainer.SizeChanged += (object sender, EventArgs e) =>
			{
				//resize the banner image to match the banner content container's height
				banner.HeightRequest = bannerContentContainer.Height;
			};

			Device.StartTimer(TimeSpan.FromMinutes(1), () => {
				if (!AuthenticationAPI.CheckToken(-1))
				{
					AuthenticationAPI.ExchangeToken();
				}
				PlayerFeedAPI.CleanUpEpisodes();
				if (GlobalResources.GetUserName() != "Guest Guest")
				{
					AuthenticationAPI.PostActionLogs();
					var check = AuthenticationAPI.GetMemberData();
				}
				return true;
			});
		}

		void OnPlayer(object o, EventArgs e) {
			//AudioPlayer.Instance.SetAudioFile(episode);
			Navigation.PushAsync(new DabPlayerPage(episode));
		}

		void OnTest(object o, EventArgs e)
		{
			Navigation.PushAsync(new DabTestContentPage());
		}

		protected override void OnDisappearing(){
			base.OnDisappearing();
			HideMenu();
		}

		void OnBrowse(object o, EventArgs e) {
			Navigation.PushAsync(new DabBrowserPage("http://c2itconsulting.net/"));
		}

		void OnChannel(object o, ItemTappedEventArgs e) {
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var selected = (Resource)e.Item;
			selected.IsNotSelected = false;
			var resource = (Resource)e.Item;
			PlayerFeedAPI.GetEpisodes(resource);
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				Navigation.PushAsync(new DabTabletPage(resource));
			}
			else
			{
				Navigation.PushAsync(new DabEpisodesPage(resource));
			}
			selected.IsNotSelected = true;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}

		//protected override void OnAppearing()
		//{
		//	base.OnAppearing();
		//	if (Device.Idiom == TargetIdiom.Tablet)
		//	{
		//		ImageService.Instance.LoadUrl(ChannelView.banner.urlTablet).DownloadOnlyAsync();
		//	}
		//	else
		//	{
		//		ImageService.Instance.LoadUrl(ChannelView.banner.urlPhone).DownloadOnlyAsync();
		//	}
		//}
	}
}
