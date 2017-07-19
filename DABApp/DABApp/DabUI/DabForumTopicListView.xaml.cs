using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumTopicListView : ContentView
	{
		public ListView topicList { get; set;}
		public Button postButton { get; set;}

		public DabForumTopicListView()
		{
			InitializeComponent();
			topicList = TopicList;
			postButton = Post;
			BackgroundColor = (Color)App.Current.Resources["PageBackgroundColor"];
		}
	}
}
