using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabUI.BaseUI;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicDetails : DabBaseContentPage
	{
		bool login = false;
		bool fromPost = false;
		Topic _topic;
		object source;

		public DabForumPhoneTopicDetails(Topic topic)
		{
			InitializeComponent();
			_topic = topic;
			DetailsView.BindingContext = topic;
			if (topic.replies.Count > 0)
			{
				DetailsView.replies.ItemsSource = topic.replies;
				DetailsView.last.Text = TimeConvert();
			}
			else 
			{
				DetailsView.replies.SeparatorVisibility = SeparatorVisibility.None;
				DetailsView.last.Text = topic.lastActivity;
			}
			DetailsView.reply.Clicked += OnReply;
			DetailsView.replies.RefreshCommand = new Command(async () => { fromPost = true; await Update(); DetailsView.replies.IsRefreshing = false;});
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
			var dateTime = DateTimeOffset.Parse(_topic.replies.OrderBy(x => x.gmtDate).Last().gmtDate + " +0:00").UtcDateTime.ToLocalTime();
			var month = dateTime.ToString("MMMM");
			var time = dateTime.ToString("t");
			return $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
		}

		async Task Update()
		{
			source = new object();
			DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));
			var result = await ContentAPI.GetTopic(_topic);
			if (result == null)
			{
				await DisplayAlert("Error, could not recieve topic details", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
			}
			else
			{
				_topic = result;
				DetailsView.replies.ItemsSource = _topic.replies;
				if (_topic.replies.Count > 0)
				{
					DetailsView.replies.SeparatorVisibility = SeparatorVisibility.Default;
					DetailsView.last.Text = TimeConvert();
				}
				fromPost = false;
			}
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
		}
	}
}
