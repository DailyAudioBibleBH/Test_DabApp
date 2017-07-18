﻿using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicList : DabBaseContentPage
	{
		bool login = false;

		public DabForumPhoneTopicList(View view, Forum forum)
		{
			InitializeComponent();
			base.ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			banner.Source = view.banner.urlPhone;
			bannerTitle.Text = view.title;
			ContentList.topicList.ItemsSource = forum.topics;
			ContentList.topicList.ItemTapped += OnTopic;
		}

		async void OnPost(object o, EventArgs e) 
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				var choice = await DisplayAlert("Log in required", "You must be logged in to make a prayer request.  Would you like to log in?", "Yes", "No");
				if (choice)
				{
					await Navigation.PushModalAsync(new DabLoginPage(true));
				}
			}
			else 
			{
				await Navigation.PushAsync(new DabForumCreateTopic());
			}
		}

		async void OnTopic(object o, ItemTappedEventArgs e) 
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var topic = (Topic)e.Item;
			var result = await ContentAPI.GetTopic(topic);
			await Navigation.PushAsync(new DabForumPhoneTopicDetails(result));
			ContentList.topicList.SelectedItem = null;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (login)
			{
				Navigation.PushAsync(new DabForumCreateTopic());
				login = false;
			}
		}
	}
}