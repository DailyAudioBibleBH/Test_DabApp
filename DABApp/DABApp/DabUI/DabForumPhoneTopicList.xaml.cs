using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.Connectivity;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicList : DabBaseContentPage
	{
		bool login = false;
		bool fromPost = false;
		bool unInitialized = true;
        int pageNumber;
		Forum _forum;
		View _view;

		public DabForumPhoneTopicList(View view)
		{
			InitializeComponent();
            pageNumber = 1;
			base.ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
			banner.Source = view.banner.urlPhone;
			bannerTitle.Text = view.title;
			_view = view;
			ContentList.topicList.ItemTapped += OnTopic;
			ContentList.postButton.Clicked += OnPost;
			ContentList.topicList.RefreshCommand = new Command(async () => { fromPost = true; await Update(); ContentList.topicList.IsRefreshing = false;});
			MessagingCenter.Subscribe<string>("topUpdate", "topUpdate", async (obj) => { await Update(); });
		}

		async void OnPost(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				if (CrossConnectivity.Current.IsConnected)
				{
					await Navigation.PushModalAsync(new DabLoginPage(true));
					login = true;
				}
				else await DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
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
			if (result == null)
			{
				await DisplayAlert("Error, could not recieve topic details", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
			}
			else
			{
				await Navigation.PushAsync(new DabForumPhoneTopicDetails(result));
			}
			ContentList.topicList.SelectedItem = null;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await Update();
			if (login && !GuestStatus.Current.IsGuestLogin)
			{
				await Navigation.PushAsync(new DabForumCreateTopic(_forum));
				fromPost = true;
				login = false;
			}
		}

		async Task Update()
		{
			if (fromPost || unInitialized)
			{
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				var result = await ContentAPI.GetForum(_view, pageNumber);
                pageNumber++;
				if (result == null)
				{
					await DisplayAlert("Error, could not retrieve topic list", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
				}
				else
				{
                    if (_forum == null)
                    {
                        _forum = result;
                    }
                    else
                    {
                        foreach (Topic t in result.topics)
                        {
                            _forum.topics.Add(t);
                        }
                    }
					ContentList.topicList.ItemsSource = _forum.topics;
					fromPost = false;
					unInitialized = false;
				}
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
			}
		}
	}
}
