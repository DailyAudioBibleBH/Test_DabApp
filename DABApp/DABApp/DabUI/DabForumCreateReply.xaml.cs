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
			NavigationPage.SetHasBackButton(this, false);
			base.ToolbarItems.Clear();
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				Container.Padding = 100;
			}
		}

		async void OnPost(object o, EventArgs e)
		{
			Post.IsEnabled = false;
			Cancel.IsEnabled = false;
			if (string.IsNullOrWhiteSpace(reply.Text))
			{
				await DisplayAlert("Cannot Post Blank Reply", "If you would like to discard your post hit cancel.", "OK");
			}
			else
			{
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
			}
			Post.IsEnabled = true;
			Cancel.IsEnabled = true;
		}

		async void OnCancel(object o, EventArgs e)
		{
			Cancel.IsEnabled = false;
			if (!string.IsNullOrEmpty(reply.Text))
			{
				var result = await DisplayAlert("Warning reply will be erased.", "Your reply is not saved locally if you navigate away from this page you will lose your work. Is that OK?", "Yes", "No");
				if (result)
				{
					await Navigation.PopAsync();
				}
			}
			else await Navigation.PopAsync();
			Cancel.IsEnabled = true;
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			MessagingCenter.Send<string>("repUpdate", "repUpdate");
		}
	}
}
