using System;
using System.Collections.Generic;
using System.Diagnostics;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerView : MenuContainerPage
	{
		//IAudio player = GlobalResources.Player;

		public DabPlayerView()
		{
			InitializeComponent();
			SeekBar.Value = AudioPlayer.Instance.CurrentTime;
			DabViewHelper.InitDabForm(this);
			//TimeBinding();
		}

		void OnPlay(object o, EventArgs e) {
			if (AudioPlayer.Instance.IsInitialized)
			{
				if (AudioPlayer.Instance.Player.IsPlaying)
				{
					AudioPlayer.Instance.Player.Pause();
					//TimeBinding();
					AudioPlayer.Instance.PlayButtonText = "Play";
				}
				else {
					AudioPlayer.Instance.Player.Play();
					//TimeBinding();
					AudioPlayer.Instance.PlayButtonText = "Pause";
				}
			}
			else {
				//AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.Player.SetAudioFile("sample.mp3");
				//GlobalResources.Player.PlayAudioFile("http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.Player.Play();
				AudioPlayer.Instance.PlayButtonText = "Pause";
			}
		}

		void OnBack30(object o, EventArgs e) {
			AudioPlayer.Instance.Player.Skip(-5);
		}

		void OnForward30(object o, EventArgs e)
		{
			AudioPlayer.Instance.Player.Skip(5);
		}

		void Handle_ValueChanged(object sender, System.EventArgs e)
		{
			switch (SegControl.SelectedSegment) {
				case 0:
					Container.BackgroundColor = Color.Red;
					Read.IsVisible = false;
					Journal.IsVisible = false;
					stackFooterContent.IsVisible = false;
					Listen.IsVisible = true;
					break;
				case 1:
					Container.BackgroundColor = Color.Black;
					Listen.IsVisible = false;
					Journal.IsVisible = false;
					if (AudioPlayer.Instance.IsInitialized)
					{
						stackFooterContent.IsVisible = true;
					}
					Read.IsVisible = true;
					break;
				case 2:
					Container.BackgroundColor = Color.Black;
					Read.IsVisible = false;
					Listen.IsVisible = false;
					if (AudioPlayer.Instance.IsInitialized)
					{
						stackFooterContent.IsVisible = true;
					}
					Journal.IsVisible = true;
					break;
			}
		}

		void OnPlayPause(object o, EventArgs e)
		{

			if (AudioPlayer.Instance.Player.IsInitialized)
			{
				if (AudioPlayer.Instance.Player.IsPlaying)
				{
					AudioPlayer.Instance.Player.Pause();
					AudioPlayer.Instance.PlayButtonText = "Play";
				}
				else {
					AudioPlayer.Instance.Player.Play();
					//ProgressBinding();
					AudioPlayer.Instance.PlayButtonText = "Pause";
				}
			}
			else {
				AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				//AudioPlayer.Instance.Player.SetAudioFile("sample.mp3");
				AudioPlayer.Instance.Player.Play();
				//ProgressBinding();
				AudioPlayer.Instance.PlayButtonText = "Pause";
			}
		}

		void OnPodcast(object o, EventArgs e) {
			SegControl.SelectTab(0);
		}
	}
}
