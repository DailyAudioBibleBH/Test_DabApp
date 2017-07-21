using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicDetails : DabBaseContentPage
	{
		bool login = false;
		bool fromPost = false;
		Topic _topic;

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
			MessagingCenter.Subscribe<string>("repUpdate", "repUpdate", (obj) => { OnAppearing(); });
		}

		async void OnReply(object o, EventArgs e)
		{ 
			if (GuestStatus.Current.IsGuestLogin)
			{
				var choice = await DisplayAlert("Log in required", "You must be logged in to reply.  Would you like to log in?", "Yes", "No");
				if (choice)
				{
					await Navigation.PushModalAsync(new DabLoginPage(true));
					login = true;
				}
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
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				var result = await ContentAPI.GetTopic(_topic);
				_topic = result;
				DetailsView.replies.ItemsSource = _topic.replies;
				DetailsView.last.Text = TimeConvert();
				if (_topic.replies.Count > 0) 
				{
					DetailsView.replies.SeparatorVisibility = SeparatorVisibility.Default;
				}
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
				fromPost = false;
			}
			if (login) 
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
	}
}
