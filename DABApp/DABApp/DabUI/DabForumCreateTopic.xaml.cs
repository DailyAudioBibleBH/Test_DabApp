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
			NavigationPage.SetHasBackButton(this, false);
			base.ToolbarItems.Clear();
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

		async void OnCancel(object o, EventArgs e)
		{
			if (!string.IsNullOrEmpty(Content.Text) || !string.IsNullOrEmpty(title.Text))
			{
				var result = await DisplayAlert("Warning reply will be erased.", "Your post is not saved locally if you navigate away from this page you will lose your work. Is that OK?", "Yes", "No");
				if (result)
				{
					await Navigation.PopAsync();
				}
			}
			else await Navigation.PopAsync();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			MessagingCenter.Send<string>("topUpdate", "topUpdate");
		}
	}
}
