using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEpisodesPage : DabBaseContentPage
	{
		Resource _resource;

		public DabEpisodesPage(Resource resource)
		{
			InitializeComponent();
			_resource = resource;
			DabViewHelper.InitDabForm(this);
			var Episodes = PlayerFeedAPI.GetEpisodeList(resource);
			EpisodeList.ItemsSource = Episodes;
			bannerImage.Source = resource.images.backgroundPhone;
			bannerContent.Text = resource.title;
			Offline.IsToggled = resource.availableOffline;
		}

		public void OnEpisode(object o, ItemTappedEventArgs e) 
		{
			var chosen = (dbEpisodes)e.Item;
			AudioPlayer.Instance.SetAudioFile(chosen.url);
			Navigation.PushAsync(new DabPlayerPage(chosen));
		}

		public void OnOffline(object o, ToggledEventArgs e) {
			_resource.availableOffline = e.Value;
			ContentAPI.UpdateOffline(e.Value, _resource.id);
		}
	}
}
