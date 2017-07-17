using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumTopicListView : ContentView
	{
		public ListView topicList { get; set;}

		public DabForumTopicListView()
		{
			InitializeComponent();
			topicList = TopicList;
			BackgroundColor = (Color)App.Current.Resources["PageBackgroundColor"];
		}
	}
}
