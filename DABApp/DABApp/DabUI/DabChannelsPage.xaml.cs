﻿using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;
using DLToolkit.Forms.Controls;
using FFImageLoading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DABApp
{
	public partial class DabChannelsPage : DabBaseContentPage
	{
		View ChannelView;
		dbEpisodes episode;
		Resource _resource;
		bool IsUnInitialized = true;

		public DabChannelsPage()
		{
			InitializeComponent();
			////Choose a different control template to disable built in scroll view
			//ControlTemplate playerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			//this.ControlTemplate = playerBarTemplate;
			DabViewHelper.InitDabForm(this);
			ChannelView = ContentConfig.Instance.views.Single(x => x.id == 56);
			//ListTitle.Text = $"<h1>{ChannelView.title}</h1>";
			BindingContext = ChannelView;
			//bannerContent.Text = ChannelView.banner.content;
			_resource = ChannelView.resources[0];
			//Container.TranslationY = -280;
			//Task.Run(async () =>
			//{
			//	await PlayerFeedAPI.GetEpisodes(_resource);
			//});
			//episode = PlayerFeedAPI.GetMostRecentEpisode(_resource);
			//if (episode == null)
			//{
			//	bannerContentContainer.IsVisible = false;
			//}
			//else
			//{
			//	bannerContentContainer.IsVisible = true;
			//	var oldText = bannerContent.Text;
			//	bannerContent.Text = oldText.Replace("[current_reading]", episode.description);
			//	if (Device.Idiom == TargetIdiom.Tablet)
			//	{
			//		bannerContentContainer.HeightRequest = 350;
			//		bannerStack.Padding = 65;
			//	}
			//}

			var remainder = ChannelView.resources.Count() % GlobalResources.Instance.FlowListViewColumns;
			var number = ChannelView.resources.Count() / GlobalResources.Instance.FlowListViewColumns;
			if (remainder != 0) {
				number += 1;
			}
			ChannelsList.HeightRequest = Device.Idiom == TargetIdiom.Tablet ? number * (GlobalResources.Instance.ThumbnailImageHeight + 60) + 120 : number * (GlobalResources.Instance.ThumbnailImageHeight + 60);

			//banner.Source = new UriImageSource
			//{
			//	Uri = new Uri((Device.Idiom == TargetIdiom.Phone ? ChannelView.banner.urlPhone : ChannelView.banner.urlTablet)),
			//	CacheValidity = GlobalResources.ImageCacheValidity
			//};

			//bannerContentContainer.SizeChanged += (object sender, EventArgs e) =>
			//{
			//	//resize the banner image to match the banner content container's height
			//	banner.HeightRequest = bannerContentContainer.Height;
			//}

			Device.StartTimer(TimeSpan.FromMinutes(30), () => {
				TimedActions();
				return true;
			});

			Device.StartTimer(TimeSpan.FromMinutes(5), () =>
			{
                if (!JournalTracker.Current.IsConnected)
                {
                    ConnectJournal();
                }
				return true;
			});
		}

		void PostLogs()
		{
			Task.Run(async () => {
				await AuthenticationAPI.PostActionLogs();
			});
		}

		async void OnPlayer(object o, EventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var reading = await PlayerFeedAPI.GetReading(episode.read_link);
			if (Device.Idiom == TargetIdiom.Tablet)
			{
                await PlayerFeedAPI.GetEpisodes(_resource); //Get episodes prior to pushing up the TabletPage
				await Navigation.PushAsync(new DabTabletPage(_resource));
			}
			else
			{
				await Navigation.PushAsync(new DabPlayerPage(episode, reading));
			}
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
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

		async void OnChannel(object o, ItemTappedEventArgs e) {
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var selected = (Resource)e.Item;
			selected.IsNotSelected = false;
			var resource = (Resource)e.Item;
			var episodes = await PlayerFeedAPI.GetEpisodes(resource); //Get episodes before pushing to the episodes page.
			if (!episodes.Contains("error") || PlayerFeedAPI.GetEpisodeList(resource).Count() > 0)
			{
				if (Device.Idiom == TargetIdiom.Tablet)
				{
                    await Navigation.PushAsync(new DabTabletPage(resource));
				}
				else
				{
					await Navigation.PushAsync(new DabEpisodesPage(resource));
				}
			}
			else await DisplayAlert("Unable to get episodes for Channel.", "This may be due to problems with your internet connection.  Please check your internet connection and try again.", "OK");
			selected.IsNotSelected = true;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}

		void TimedActions()
		{ 
			if (!AuthenticationAPI.CheckToken(-1))
				{
					Task.Run(async() =>
					{
						await AuthenticationAPI.ExchangeToken();
					});
				}
				PlayerFeedAPI.CleanUpEpisodes();
			Task.Run(async () =>
			{
				await PlayerFeedAPI.DownloadEpisodes();
			});
			if (GlobalResources.GetUserName() != "Guest Guest")
			{
				Task.Run(async () =>
				{
					await AuthenticationAPI.PostActionLogs();
					await AuthenticationAPI.GetMemberData();
				});
			}
		}

		void ConnectJournal()
		{
			AuthenticationAPI.ConnectJournal();
		}

		protected override async void OnAppearing()
		{
			MessagingCenter.Send<string>("Setup", "Setup");
		//	var start = DateTime.Now;
		    base.OnAppearing();
            TimedActions();
		//	if (episode == null)
		//	{
		//		bannerButton.IsEnabled = false;
		//	}
		//	Debug.WriteLine($"Before getting episodes {(DateTime.Now - start).TotalMilliseconds}");
		//	await PlayerFeedAPI.GetEpisodes(_resource);
		//	Debug.WriteLine($"Finished GetEpisodes call {(DateTime.Now - start).TotalMilliseconds}");
		//	episode = await PlayerFeedAPI.GetMostRecentEpisode(_resource);
		//	Debug.WriteLine($"Finished Getting Most Recent Episode {(DateTime.Now - start).TotalMilliseconds}");
		//	if (episode == null)
		//	{
		//		bannerContentContainer.IsVisible = false;
		//	}
		//	else
		//	{
		//		double height = 300;
		//		var oldText = bannerContent.Text;
		//		bannerContent.Text = oldText.Replace("[current_reading]", episode.description);
		//		Debug.WriteLine($"Finished replacing episode description {(DateTime.Now - start).TotalMilliseconds}");
		//		if (Device.Idiom == TargetIdiom.Tablet)
		//		{
		//			height = 350;
		//			bannerContentContainer.HeightRequest = height;
		//			bannerStack.Padding = 65;
		//		}
		//		bannerContentContainer.IsVisible = true;
		//		if (IsUnInitialized)
		//		{
		//			bannerContentContainer.IsVisible = true;
		//			await Container.TranslateTo(0, 0, 500, Easing.Linear);
		//			IsUnInitialized = false;
		//		}
		//	}
		//	bannerButton.IsEnabled = true;
		}
	}
}
