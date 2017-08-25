using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabEpisodesPage : DabBaseContentPage
	{
		Resource _resource;
		IEnumerable<dbEpisodes> Episodes;

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
			Months.Items.Add("My Journaled Episodes");
			Months.Items.Add("My Favorites");
			Months.SelectedIndex = 0;
			Device.StartTimer(TimeSpan.FromSeconds(5), () =>
			{
				EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
				return true;
			});
		}

		public async void OnEpisode(object o, ItemTappedEventArgs e) 
		{
			//activityHolder.IsVisible = true;
			//activity.IsVisible = true;
			var chosen = (dbEpisodes)e.Item;
			EpisodeList.SelectedItem = null;
			var _reading = await PlayerFeedAPI.GetReading(chosen.read_link);
			//if (AudioPlayer.Instance.CurrentEpisodeId != chosen.id)
			//{
			//	AudioPlayer.Instance.SetAudioFile(chosen);
			//}
			await Navigation.PushAsync(new DabPlayerPage(chosen, _reading));
			EpisodeList.SelectedItem = null;
			//activityHolder.IsVisible = false;
			//activity.IsVisible = false;
		}

		public void OnOffline(object o, ToggledEventArgs e) {
			_resource.availableOffline = e.Value;
			ContentAPI.UpdateOffline(e.Value, _resource.id);
			if (e.Value)
			{
				Task.Run(async () => { await PlayerFeedAPI.DownloadEpisodes(); });
			}
			else {
				PlayerFeedAPI.DeleteChannelEpisodes(_resource);
			}
		}

		public void OnMonthSelected(object o, EventArgs e) {
			if ((string)Months.SelectedItem != "My Favorites" || (string)Months.SelectedItem != "My Journaled Episodes")
			{
				EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
			}
			else 
			{
				if ((string)Months.SelectedItem == "My Favorites")
				{
					EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite == true);
				}
				else 
				{
					EpisodeList.ItemsSource = Episodes.Where(x => x.has_journal == true);
				}
			}
		}
	}
}
