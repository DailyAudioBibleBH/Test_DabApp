using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEpisodesPage : DabBaseContentPage
	{
		public DabEpisodesPage(Resource resource)
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			var Episodes = PlayerFeedAPI.GetEpisodeList(resource);
			EpisodeList.ItemsSource = Episodes;
			bannerImage.Source = resource.images.backgroundPhone;
			bannerContent.Text = resource.title;
		}
	}
}
