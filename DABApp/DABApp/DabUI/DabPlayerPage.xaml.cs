using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerPage : DabBaseContentPage
	{
		//IAudio player = GlobalResources.Player;
		dbEpisodes Episode;
		string backgroundImage;
		string playImage;
		string pauseImage;
		string forwardImage;
		string backwardImage;

		public DabPlayerPage(dbEpisodes episode)
		{
			InitializeComponent();

			Episode = episode;
			//AudioPlayer.Instance.ShowPlayerBar = false;
			SeekBar.Value = AudioPlayer.Instance.CurrentTime;
			DabViewHelper.InitDabForm(this);
			backgroundImage = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == episode.channel_title).images.backgroundPhone;
			BackgroundImage.Source = backgroundImage;
			BindingContext = episode;
			//Date.Text = $"{episode.PubMonth} {episode.PubDay.ToString()} {episode.PubYear.ToString()}";
			base.ControlTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			if (Device.OS == TargetPlatform.iOS) {
				playImage = "ic_play_circle_outline_white_3x.png";
				pauseImage = "ic_pause_circle_outline_white_3x.png";
				forwardImage = "ic_forward_30_white_2x.png";
				backwardImage = "ic_replay_30_white_2x.png";
			}
			else{
				playImage = "ic_play_circle_outline_white.png";
				pauseImage = "ic_pause_circle_outline_white.png";
				forwardImage = "ic_forward_30_white.png";
				backwardImage = "ic_replay_30_white.png";
			}
			backwardButton.Image = backwardImage;
			forwardButton.Image = forwardImage;
			if (AudioPlayer.Instance.IsPlaying)
			{
				AudioPlayer.Instance.PlayButtonImage = pauseImage;
			}
			else AudioPlayer.Instance.PlayButtonImage = playImage;
		}

		void OnPlay(object o, EventArgs e)
		{
			if (AudioPlayer.Instance.IsInitialized)
			{
				if (AudioPlayer.Instance.Player.IsPlaying)
				{
					AudioPlayer.Instance.Player.Pause();
					//TimeBinding();
					AudioPlayer.Instance.PlayButtonImage = playImage;
				}
				else {
					AudioPlayer.Instance.Player.Play();
					//TimeBinding();
					AudioPlayer.Instance.PlayButtonImage = pauseImage;
				}
			}
			else {
				//AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.SetAudioFile(Episode);
				//GlobalResources.Player.PlayAudioFile("http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.Player.Play();
				AudioPlayer.Instance.PlayButtonImage = pauseImage;
			}
		}

		void OnBack30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Player.Skip(-30);
		}

		void OnForward30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Player.Skip(30);
		}

		void Handle_ValueChanged(object sender, System.EventArgs e)
		{
			switch (SegControl.SelectedSegment)
			{
				case 0:
					Read.IsVisible = false;
					Journal.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = false;
					Listen.IsVisible = true;
					BackgroundImage.IsVisible = true;
					break;
				case 1:
					Listen.IsVisible = false;
					Journal.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = true;
					Read.IsVisible = true;
					BackgroundImage.IsVisible = false;
					break;
				case 2:
					Read.IsVisible = false;
					Listen.IsVisible = false;
					//AudioPlayer.Instance.showPlayerBar = true;
					Journal.IsVisible = true;
					BackgroundImage.IsVisible = false;
					break;
			}
		}

		//void OnPlayPause(object o, EventArgs e)
		//{

		//	if (AudioPlayer.Instance.Player.IsInitialized)
		//	{
		//		if (AudioPlayer.Instance.Player.IsPlaying)
		//		{
		//			AudioPlayer.Instance.Player.Pause();
		//			AudioPlayer.Instance.PlayButtonImage = playImage;
		//		}
		//		else {
		//			AudioPlayer.Instance.Player.Play();
		//			//ProgressBinding();
		//			AudioPlayer.Instance.PlayButtonImage = pauseImage;
		//		}
		//	}
		//	else {
		//		//AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
		//		AudioPlayer.Instance.Player.SetAudioFile("@"+Episode.url);
		//		AudioPlayer.Instance.Player.Play();
		//		//ProgressBinding();
		//		AudioPlayer.Instance.PlayButtonImage = pauseImage;
		//	}
		//}

		void OnPodcast(object o, EventArgs e)
		{
			SegControl.SelectTab(0);
		}

		void OnSaveJournal(object o, EventArgs e) { 
			
		}
	}
}
