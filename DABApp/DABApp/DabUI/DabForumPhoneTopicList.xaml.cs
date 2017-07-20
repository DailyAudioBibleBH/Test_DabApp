using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicList : DabBaseContentPage
	{
		bool login = false;
		bool fromPost = false;
		Forum _forum;
		View _view;

		public DabForumPhoneTopicList(View view, Forum forum)
		{
			InitializeComponent();
			base.ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			banner.Source = view.banner.urlPhone;
			bannerTitle.Text = view.title;
			_forum = forum;
			_view = view;
			ContentList.topicList.ItemsSource = _forum.topics;
			ContentList.topicList.ItemTapped += OnTopic;
			ContentList.postButton.Clicked += OnPost;
			MessagingCenter.Subscribe<string>("topUpdate", "topUpdate", async (obj) => { await Update(); });
		}

		async void OnPost(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				var choice = await DisplayAlert("Log in required", "You must be logged in to make a prayer request.  Would you like to log in?", "Yes", "No");
				if (choice)
				{
					await Navigation.PushModalAsync(new DabLoginPage(true));
					login = true;
				}
			}
			else
			{
				await Navigation.PushAsync(new DabForumCreateTopic(_forum));
				fromPost = true;
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

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await Update();
			if (login)
			{
				await Navigation.PushAsync(new DabForumCreateTopic(_forum));
				fromPost = true;
				login = false;
			}
		}

		async Task Update()
		{
			if (fromPost)
			{
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				var result = await ContentAPI.GetForum(_view);
				_forum = result;
				ContentList.topicList.ItemsSource = result.topics;
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
				fromPost = false;
			}
		}
	}
}
