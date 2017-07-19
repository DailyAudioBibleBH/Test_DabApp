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
			Content.HeightRequest = this.Height;
			_forum = forum;
		}

		async void OnPost(object o, EventArgs e)
		{
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
		}
	}
}
