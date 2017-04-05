using System;
using System.Collections.Generic;
using System.Linq;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEpisodesPage : DabBaseContentPage
	{
		Resource _resource;
		List<dbEpisodes> Episodes;

		public DabEpisodesPage(Resource resource)
		{
			InitializeComponent();
			_resource = resource;
			DabViewHelper.InitDabForm(this);
			Episodes = PlayerFeedAPI.GetEpisodeList(resource);
			//EpisodeList.ItemsSource = Episodes;
			bannerImage.Source = resource.images.bannerPhone;
			bannerContent.Text = resource.title;
			Offline.IsToggled = resource.availableOffline;
			var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
			foreach (var month in months) {
				Months.Items.Add(month);
			}
			Months.SelectedIndex = 0;
			EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
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

		public void OnMonthSelected(object o, EventArgs e) {
			EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
		}
	}
}
