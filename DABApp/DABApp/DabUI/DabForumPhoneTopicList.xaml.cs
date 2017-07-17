using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumPhoneTopicList : DabBaseContentPage
	{
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
			
		}

		async void OnTopic(object o, ItemTappedEventArgs e) 
		{ 
			
		}
	}
}
