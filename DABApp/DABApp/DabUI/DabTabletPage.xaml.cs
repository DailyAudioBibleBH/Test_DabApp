using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTabletPage : DabBaseContentPage
	{
		Resource _resource;
		IEnumerable<dbEpisodes> Episodes;
		string backgroundImage;
		dbEpisodes episode;

		public DabTabletPage(Resource resource)
		{
			InitializeComponent();
			_resource = resource;
			ChannelsList.ItemsSource = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources;
			ChannelsList.SelectedItem = _resource;
			backgroundImage = _resource.images.backgroundTablet;
			BackgroundImage.Source = backgroundImage;
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			var months = Episodes.Select(x => x.PubMonth).Distinct().ToList();
			foreach (var month in months) {
				Months.Items.Add(month);
			}
			Months.SelectedIndex = 0;
			Device.StartTimer( TimeSpan.FromSeconds(5),() =>
			{
				Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
				EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
				episode = Episodes.First();
				PlayerLabels.BindingContext = episode;
				Read.BindingContext = episode;
				Journal.BindingContext = episode;
				return true;
			});

		}

		void Handle_ValueChanged(object sender, System.EventArgs e)
		{
			switch (SegControl.SelectedSegment)
			{
				case 0:
					Read.IsVisible = false;
					Journal.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = false;
					Archive.IsVisible = true;
					break;
				case 1:
					Archive.IsVisible = false;
					Journal.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = true;
					Read.IsVisible = true;
					break;
				case 2:
					Read.IsVisible = false;
					Archive.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = true;
					Journal.IsVisible = true;
					break;
			}
		}

		public void OnEpisode(object o, ItemTappedEventArgs e)
		{
			episode = (dbEpisodes)e.Item;
			EpisodeList.SelectedItem = null;
			if (AudioPlayer.Instance.CurrentEpisodeId != episode.id)
			{
				AudioPlayer.Instance.SetAudioFile(episode);
				PlayerLabels.BindingContext = episode;
			}
			EpisodeList.SelectedItem = null;
		}

		public void OnOffline(object o, ToggledEventArgs e)
		{
			_resource.availableOffline = e.Value;
			ContentAPI.UpdateOffline(e.Value, _resource.id);
			if (e.Value)
			{
				Task.Run(async () => { await PlayerFeedAPI.DownloadEpisodes(); });
			}
			else
			{
				PlayerFeedAPI.DeleteChannelEpisodes(_resource);
			}
		}

		public void OnMonthSelected(object o, EventArgs e)
		{
			EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
		}

		void OnChannel(object o, EventArgs e) {
			_resource = (Resource)ChannelsList.SelectedItem;
			backgroundImage = _resource.images.backgroundTablet;
			Episodes = PlayerFeedAPI.GetEpisodeList(_resource);
			EpisodeList.ItemsSource = Episodes;
			BackgroundImage.Source = backgroundImage;
			episode = Episodes.First();
			AudioPlayer.Instance.SetAudioFile(episode);
		}

		void OnBack30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Skip(-30);
		}

		void OnForward30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Skip(30);
		}

		void OnPlay(object o, EventArgs e)
		{
			if (AudioPlayer.Instance.IsInitialized)
			{
				if (AudioPlayer.Instance.IsPlaying)
				{
					AudioPlayer.Instance.Pause();
				}
				else
				{
					AudioPlayer.Instance.Play();
				}
			}
			else
			{
				AudioPlayer.Instance.SetAudioFile(episode);
				AudioPlayer.Instance.Play();
			}
		}

		void OnShare(object o, EventArgs e) { 
			Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent(episode.channel_code, episode.id.ToString());
		}
	}
}
