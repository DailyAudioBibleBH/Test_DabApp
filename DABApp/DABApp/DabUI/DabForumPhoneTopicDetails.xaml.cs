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
			if (topic.replies.Count > 0)
			{
				DetailsView.replies.ItemsSource = topic.replies;
			}
			else
			{
				DetailsView.replies.SeparatorVisibility = SeparatorVisibility.None;
			}
			DetailsView.last.Text = topic.lastActivity;
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
