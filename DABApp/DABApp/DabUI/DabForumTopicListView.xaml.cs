using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumTopicListView : ContentView, INotifyPropertyChanged
	{
		public ListView topicList { get; set; }
		public Button postButton { get; set;}

		public DabForumTopicListView()
		{
			InitializeComponent();
			topicList = TopicList;
			postButton = Post;
			BackgroundColor = (Color)App.Current.Resources["PageBackgroundColor"];
			if (GuestStatus.Current.IsGuestLogin)
			{
				Post.Text = "  Log in  ";
			}
		}
    }
}
