using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DABApp.DabUI.BaseUI;
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
		object source;

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
			ContentList.topicList.RefreshCommand = new Command(async () => { fromPost = true; await Update(); ContentList.topicList.IsRefreshing = false; });
			MessagingCenter.Subscribe<string>("topUpdate", "topUpdate", async (obj) => { await Update(); });
		}

		async void OnPost(object o, EventArgs e)
		{
			if (GuestStatus.Current.IsGuestLogin)
			{
				//Take them back to log on so they can get back to this.
				GlobalResources.LogoffAndResetApp();
			}
			else
			{
				await Navigation.PushAsync(new DabForumCreateTopic(_forum));
				fromPost = true;
			}
		}

		async void OnTopic(object o, ItemTappedEventArgs e)
		{
			DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
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
			DabUserInteractionEvents.WaitStopped(source, new EventArgs());
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
				source = new object();
				DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Please Wait...", true));
				_forum = await ContentAPI.GetForum(_view, pageNumber);
				if (_forum == null)
				{
					await DisplayAlert("Error, could not retrieve topic list", "This may be due to loss of connectivity.  Please check your internet settings and try again.", "OK");
				}
				else
				{
					ContentList.topicList.BindingContext = _forum;
					ContentList.topicList.ItemsSource = _forum.topics;
					fromPost = false;
					unInitialized = false;
				}
				DabUserInteractionEvents.WaitStopped(source, new EventArgs());
			}
		}
	}
}