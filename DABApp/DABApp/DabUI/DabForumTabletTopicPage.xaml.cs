using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumTabletTopicPage : DabBaseContentPage
	{
		bool login = false;
		bool fromPost = false;
		Forum _forum;
		Topic topic;
		View _view;

		public DabForumTabletTopicPage(View view, Forum forum)
		{
			InitializeComponent();
			ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			banner.Source = view.banner.urlTablet;
			bannerTitle.Text = view.title;
			_forum = forum;
			_view = view;
			BindingContext = view;
			ContentList.topicList.ItemsSource = forum.topics;
			ContentList.topicList.ItemTapped += OnTopic;
		}

		async void OnTopic(object o, ItemTappedEventArgs e)
		{
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			topic = (Topic)e.Item;
			DetailsView.BindingContext = topic;
			var result = await ContentAPI.GetTopic(topic);
			DetailsView.replies.ItemsSource = result.replies;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}
	}
}
