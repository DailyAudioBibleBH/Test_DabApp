using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabOfflineEpisodeManagementPage : DabBaseContentPage
	{
		public DabOfflineEpisodeManagementPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			List<Preset> durations = new List<Preset>() { new Preset("One Day", false), new Preset("Two Days", false), new Preset("Three Days", false), new Preset("One Week", false), new Preset("One Month", false) };
			foreach (Preset preset in durations) {
				if (preset.duration == OfflineEpisodeSettings.Instance.Duration) {
					preset.Selected = true;
				}
			}
			Durations.ItemsSource = durations;
		}

		void OnDeleteAfterListening(object o, ToggledEventArgs e) {
			var pre = e.Value;
			OfflineEpisodeSettings.Instance.DeleteAfterListening = pre;
			PlayerFeedAPI.UpdateOfflineEpisodeSettings();
		}

		void OnDurationPicked(object o, ItemTappedEventArgs e) {
			var pre = e.Item as Preset;
			foreach (Preset preset in Durations.ItemsSource) {
				preset.Selected = preset == pre;
			}
			OfflineEpisodeSettings.Instance.Duration = pre.duration;
			PlayerFeedAPI.UpdateOfflineEpisodeSettings();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			PlayerFeedAPI.CleanUpEpisodes();
		}
	}
}
