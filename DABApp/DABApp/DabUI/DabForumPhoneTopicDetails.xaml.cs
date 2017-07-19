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
		Topic _topic;

		public DabForumPhoneTopicDetails(Topic topic)
		{
			InitializeComponent();
			_topic = topic;
			DetailsView.BindingContext = topic;
			DetailsView.replies.ItemsSource = topic.replies;
			var dateTime = Convert.ToDateTime(topic.replies.OrderBy(x => x.gmtDate).First().gmtDate);
			var month = dateTime.ToString("MMMM");
			var time = dateTime.TimeOfDay.ToString();
			DetailsView.last.Text = $"{month} {dateTime.Day}, {dateTime.Year} at {time}";
			DetailsView.reply.Clicked += OnReply;
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
			}
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (login) 
			{
				Navigation.PushAsync(new DabForumCreateReply(_topic));
				login = false;
			}
		}
	}
}
