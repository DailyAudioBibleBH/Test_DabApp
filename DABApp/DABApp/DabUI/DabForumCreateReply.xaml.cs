using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabForumCreateReply : DabBaseContentPage
	{
		Topic _topic;

		public DabForumCreateReply(Topic topic)
		{
			InitializeComponent();
			BindingContext = topic;
			_topic = topic;
		}

		async void OnPost(object o, EventArgs e)
		{
			Post.IsEnabled = false;
			var rep = new PostReply(reply.Text, _topic.id);
			var result = await ContentAPI.PostReply(rep);
			if (result.Contains("id"))
			{
				await DisplayAlert("Success", "Successfully posted new reply.", "OK");
				await Navigation.PopAsync();
			}
			else 
			{
				await DisplayAlert("Error", result, "OK");
			}
			Post.IsEnabled = true;
		}
	}
}
