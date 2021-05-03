using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using Plugin.Connectivity;
using Xamarin.Forms;
using static DABApp.ContentConfig;

namespace DABApp
{
	public partial class DabForumTabletTopicPage : DabBaseContentPage
	{
		bool loginRep = false;
		bool loginTop = false;
		bool unInitialized = true;
		Forum _forum;
		DabGraphQlTopic topic;
		ObservableCollection<DabGraphQlReply> replies;
		object source;

		public DabForumTabletTopicPage(View view)
		{
			InitializeComponent();
			replies = new ObservableCollection<DabGraphQlReply>();
			ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			banner.Source = view.banner.urlTablet;
			bannerTitle.Text = view.title;
			BindingContext = view;
			ContentList.topicList.ItemTapped += OnTopic;
			ContentList.postButton.Clicked += OnPost;
			DetailsView.reply.Clicked += OnReply;
			ContentList.topicList.RefreshCommand = new Command(async () => { await Update(); ContentList.topicList.IsRefreshing = false; });
			DetailsView.replies.RefreshCommand = new Command(async () => { await Update(); DetailsView.replies.IsRefreshing = false; });
			MessagingCenter.Subscribe<string>("repUpdate", "repUpdate", (obj) => { OnAppearing(); });
			MessagingCenter.Subscribe<string>("topUpdate", "topUpdate", (obj) => { OnAppearing(); });
		}

		async void OnTopic(object o, ItemTappedEventArgs e)
		{
			topic = (DabGraphQlTopic)e.Item;
			DetailsView.BindingContext = topic;
			DetailsView.IsVisible = true;
			var replyData = await DabService.GetUpdatedReplies(DateTime.MinValue, topic.wpId, 30);
            foreach (var item in replyData.Data)
            {
				replies = new ObservableCollection<DabGraphQlReply>(item.payload.data.updatedReplies.edges.Where(x => x.status == "publish").OrderBy(x => x.createdAt));
			}
			replies.OrderByDescending(x => x.createdAt);
			DetailsView.replies.ItemsSource = replies;
			DetailsView.last.Text = TimeConvert();
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
		}

		async void OnPost(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				GlobalResources.LogoffAndResetApp();

			}
			else
			{
				await Navigation.PushAsync(new DabForumCreateTopic(_forum));
			}
		}

		async void OnReply(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				await GlobalResources.LogoffAndResetApp();
			}
			else
			{
				await Navigation.PushAsync(new DabForumCreateReply(topic));
			}
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			ContentList.topicList.IsRefreshing = true;
			await Update();
			ContentList.topicList.IsRefreshing = false;
			
			if (!GuestStatus.Current.IsGuestLogin)
			{
				if (loginRep)
				{
					await Navigation.PushAsync(new DabForumCreateReply(topic));
					loginRep = false;
				}
				if (loginTop)
				{
					await Navigation.PushAsync(new DabForumCreateTopic(_forum));
					loginTop = false;
				}
			}
		}

		string TimeConvert()
		{
			if (replies.Count() > 0)
			{;

				var dateTime = topic.createdAt.ToLocalTime();
				var month = dateTime.ToString("MMMM");
				var time = dateTime.ToString("t");
				return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
			}
            else
            {
				return "";
			}
		}

		async Task Update()
		{
			source = new object();
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));
			_forum = await DabService.GetForum(ContentList.topicList.IsRefreshing);
            if (_forum.topicCount > 0 && topic == null)
            {
				topic = _forum.topics.FirstOrDefault();
			}
			if (topic != null)
			{
				if (topic == null)
				{
					await DisplayAlert("Error, could not recieve topic details", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
				}
				else
				{
					DetailsView.BindingContext = topic;
					DetailsView.IsVisible = true;

					//Attach replies to details view
					var replyData = await DabService.GetUpdatedReplies(DateTime.MinValue, topic.wpId, 30);

					foreach (var item in replyData.Data)
					{
						replies = new ObservableCollection<DabGraphQlReply>(item.payload.data.updatedReplies.edges.Where(x => x.status == "publish").OrderBy(x => x.createdAt));
					}

					DetailsView.replies.ItemsSource = replies;
					DetailsView.last.Text = TimeConvert();

					if (topic.replyCount > 0)
					{
						DetailsView.replies.SeparatorVisibility = SeparatorVisibility.Default;
					}
				}
			}
			if (_forum == null)
			{
				await DisplayAlert("Error, could not recieve topic list", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
			}
			else
			{
				ContentList.BindingContext = _forum;
				ContentList.topicList.ItemsSource = _forum.topics;
			}
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
			unInitialized = false;
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			banner.Aspect = width > height ? Aspect.Fill : Aspect.AspectFill;
		}
	}
}