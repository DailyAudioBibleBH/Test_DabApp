using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumCreateTopic : DabBaseContentPage
	{
		Forum _forum;

		public DabForumCreateTopic(Forum forum)
		{
			InitializeComponent();
			Content.HeightRequest = 250;
			_forum = forum;
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				Container.Padding = 100;
			}
		}

		async void OnPost(object o, EventArgs e)
		{
			Post.IsEnabled = false;
			var topic = new PostTopic(title.Text, Content.Text, _forum.id);
			var result = await ContentAPI.PostTopic(topic);
			if (result.Contains("id"))
			{
				await DisplayAlert("Success", "Successfully posted new topic.", "OK");
				await Navigation.PopAsync();
			}
			else 
			{
				await DisplayAlert("Error", result, "OK");
			}
			Post.IsEnabled = true;
		}

		void OnTitle(object o, EventArgs e)
		{
			Content.Focus();
		}

		void OnContent(object o, EventArgs e)
		{
			OnPost(o, e);
		}
	}
}
