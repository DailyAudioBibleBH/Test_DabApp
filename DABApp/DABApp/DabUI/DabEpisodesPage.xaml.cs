using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Connectivity;
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
			Months.Items.Add("My Journals");
			Months.Items.Add("My Favorites");
			Months.SelectedIndex = 0;
			Device.StartTimer(TimeSpan.FromSeconds(5), () =>
			{
				if ((string)Months.SelectedItem == "My Favorites")
				{
					EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite);
				}
				else
				{
					if ((string)Months.SelectedItem == "My Journals")
					{
						EpisodeList.ItemsSource = Episodes.Where(x => x.has_journal == true);
					}
					else
					{
						EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
					}
				}
				return true;
			});
		}

		public async void OnEpisode(object o, ItemTappedEventArgs e)
		{
			EpisodeList.IsEnabled = false;
			ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
			StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
			activity.IsVisible = true;
			activityHolder.IsVisible = true;
			var chosen = (dbEpisodes)e.Item;
			EpisodeList.SelectedItem = null;
			var _reading = await PlayerFeedAPI.GetReading(chosen.read_link);
			if (chosen.is_downloaded || CrossConnectivity.Current.IsConnected)
			{
				await Navigation.PushAsync(new DabPlayerPage(chosen, _reading));
			}
			else await DisplayAlert("Unable to stream episode.", "To ensure episodes can be played offline download them before going offline.", "OK");
			EpisodeList.SelectedItem = null;
			activity.IsVisible = false;
			activityHolder.IsVisible = false;
			EpisodeList.IsEnabled = true;
		}

		public void OnOffline(object o, ToggledEventArgs e) {
			_resource.availableOffline = e.Value;
			ContentAPI.UpdateOffline(e.Value, _resource.id);
			if (e.Value)
			{
				Task.Run(async () => { await PlayerFeedAPI.DownloadEpisodes(); });
			}
			else {
				Task.Run(async () => { await PlayerFeedAPI.DeleteChannelEpisodes(_resource); });
			}
		}

		public void OnMonthSelected(object o, EventArgs e) {
			if ((string)Months.SelectedItem == "My Favorites")
			{
				EpisodeList.ItemsSource = Episodes.Where(x => x.is_favorite == true);
			}
			else 
			{
				if ((string)Months.SelectedItem == "My Journals")
				{
					EpisodeList.ItemsSource = Episodes.Where(x => x.has_journal == true);
				}
				else 
				{
					EpisodeList.ItemsSource = Episodes.Where(x => x.PubMonth == Months.Items[Months.SelectedIndex]);
				}
			}
		}
	}
}
