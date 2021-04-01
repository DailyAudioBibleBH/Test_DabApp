using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
	public partial class DabForumPhoneTopicDetails : DabBaseContentPage
	{
		bool login = false;
		bool fromPost = false;
		DabGraphQlTopic _topic;
		object source;
		ObservableCollection<DabGraphQlReply> replies;

		public DabForumPhoneTopicDetails(DabGraphQlTopic topic)
		{
			InitializeComponent();
			_topic = topic;
			DetailsView.BindingContext = topic;
			replies = new ObservableCollection<DabGraphQlReply>();

			if (topic.replyCount > 0)
			{
				DetailsView.replies.ItemsSource = replies;
				DetailsView.last.Text = TimeConvert();
			}
			else
			{
				DetailsView.replies.SeparatorVisibility = SeparatorVisibility.None;
				DetailsView.last.Text = topic.lastActive.ToString();
			}
			DetailsView.reply.Clicked += OnReply;
			DetailsView.replies.RefreshCommand = new Command(async () => { fromPost = true; await Update(); DetailsView.replies.IsRefreshing = false; });
			MessagingCenter.Subscribe<string>("repUpdate", "repUpdate", (obj) => { OnAppearing(); });
		}

		async void OnReply(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				GlobalResources.LogoffAndResetApp();
			}
			else
			{
				await Navigation.PushAsync(new DabForumCreateReply(_topic));
				fromPost = true;
			}
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			var result = await DabService.GetUpdatedReplies(DateTime.MinValue, _topic.wpId, 30);

            if (result.Success)
            {
                foreach (var item in result.Data)
                {
					replies = new ObservableCollection<DabGraphQlReply>(item.payload.data.updatedReplies.edges.Where(x => x.status == "publish"));
                }
				DetailsView.replies.ItemsSource = replies;
				DetailsView.last.Text = TimeConvert();
			}

			if (fromPost)
			{
				await Update();
			}
			if (login && !GuestStatus.Current.IsGuestLogin)
			{
				await Navigation.PushAsync(new DabForumCreateReply(_topic));
				login = false;
				fromPost = true;
			}
		}

		string TimeConvert()
		{
            if (replies.Count() > 0)
            {
				var dateTime = DateTimeOffset.Parse(replies.OrderBy(x => x.createdAt).Last().createdAt + " +0:00").UtcDateTime.ToLocalTime();
				var month = dateTime.ToString("MMMM");
				var time = dateTime.ToString("t");
				return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
			}
            else
            {
				var dateTime = DateTimeOffset.Parse(_topic.lastActive + " +0:00").UtcDateTime.ToLocalTime();
				var month = dateTime.ToString("MMMM");
				var time = dateTime.ToString("t");
				return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
            }            
		}

		async Task Update()
		{
			source = new object();
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));

			var result = await DabService.GetUpdatedReplies(DateTime.MinValue, _topic.wpId, 30);
			if (result.Success)
			{
				foreach (var item in result.Data)
				{
					replies = new ObservableCollection<DabGraphQlReply>(item.payload.data.updatedReplies.edges.Where(x => x.status == "publish"));
				}
				
				fromPost = false;
			}
			else
            {
				await DisplayAlert("Error, could not recieve topic details", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
			}

			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
		}
	}
}