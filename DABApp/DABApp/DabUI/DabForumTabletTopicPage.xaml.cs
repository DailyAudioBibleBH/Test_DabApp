using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabUI.BaseUI;
using Plugin.Connectivity;
using Xamarin.Forms;
using static DABApp.ContentConfig;

namespace DABApp
{
	public partial class DabForumTabletTopicPage : DabBaseContentPage
	{
		bool loginRep = false;
		bool loginTop = false;
		bool fromPost = false;
		bool unInitialized = true;
		int pageNumber;
		Forum _forum;
		Topic topic;
		View _view;
		object source;

		public DabForumTabletTopicPage(View view)
		{
			InitializeComponent();
			pageNumber = 1;
			ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			banner.Source = view.banner.urlTablet;
			bannerTitle.Text = view.title;
			_view = view;
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
			topic = (Topic)e.Item;
			DetailsView.BindingContext = topic;
			DetailsView.IsVisible = true;
			topic = await ContentAPI.GetTopic(topic);
			DetailsView.replies.ItemsSource = topic.replies;
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
				fromPost = true;
			}
		}

		async void OnReply(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				GlobalResources.LogoffAndResetApp();

			}
			else
			{
				await Navigation.PushAsync(new DabForumCreateReply(topic));
				fromPost = true;
			}
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			if (fromPost || unInitialized)
			{
				await Update();
			}
			if (!GuestStatus.Current.IsGuestLogin)
			{
				if (loginRep)
				{
					await Navigation.PushAsync(new DabForumCreateReply(topic));
					fromPost = true;
					loginRep = false;
				}
				if (loginTop)
				{
					await Navigation.PushAsync(new DabForumCreateTopic(_forum));
					fromPost = true;
					loginTop = false;
				}
			}
		}

		string TimeConvert()
		{
			if (topic.replies.Count > 0)
			{
				var dateTime = DateTimeOffset.Parse(topic.replies.OrderBy(x => x.gmtDate).Last().gmtDate + " +0:00").UtcDateTime.ToLocalTime();
				var month = dateTime.ToString("MMMM");
				var time = dateTime.ToString("t");
				return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
			}
			return "";
		}

		async Task Update()
		{
			source = new object();
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));
			if (topic != null)
			{
				topic = await ContentAPI.GetTopic(topic);
				if (topic == null)
				{
					await DisplayAlert("Error, could not recieve topic details", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
				}
				else
				{
					DetailsView.replies.ItemsSource = topic.replies;
					DetailsView.last.Text = TimeConvert();
					if (topic.replies.Count > 0)
					{
						DetailsView.replies.SeparatorVisibility = SeparatorVisibility.Default;
					}
				}
			}
			_forum = await ContentAPI.GetForum(_view);
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
			fromPost = false;
			unInitialized = false;
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			banner.Aspect = width > height ? Aspect.Fill : Aspect.AspectFill;
		}
	}
}