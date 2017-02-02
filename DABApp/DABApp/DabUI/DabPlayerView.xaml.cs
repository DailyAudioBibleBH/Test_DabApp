using System;
using System.Collections.Generic;
using System.Diagnostics;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerView : MenuContainerPage
	{
		IAudio player = GlobalResources.Player;

		public DabPlayerView()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			TimeBinding();
		}

		void OnPlay(object o, EventArgs e) {
			if (player.IsInitialized)
			{
				if (player.IsPlaying)
				{
					player.Pause();
					TimeBinding();
					AudioPlayer.Instance.PlayButtonText = "Play";
				}
				else {
					player.Play();
					TimeBinding();
					AudioPlayer.Instance.PlayButtonText = "Pause";
				}
			}
			else {
				player.SetAudioFile("sample.mp3");
				//GlobalResources.Player.PlayAudioFile("http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				player.Play();
				AudioPlayer.Instance.PlayButtonText = "Pause";
			}
		}

		void OnBack30(object o, EventArgs e) {
			SeekTo(-5);
		}

		void OnForward30(object o, EventArgs e) {
			SeekTo(5);
		}

		void TimeBinding() { 
			Device.StartTimer(new TimeSpan(0, 0, 0, 0, 1), () =>
			{
				if (player.IsInitialized)
				{
					SeekBar.Maximum = player.TotalTime;
					SeekBar.Value = player.CurrentTime;
					CurrentTime.Text = player.CurrentTime.ToString("##.##");
					//var remaining = TimeSpan.FromMilliseconds(player.RemainingTime());
					RemainingTime.Text = player.RemainingTime.ToString("##.##");//string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds);
					return player.IsPlaying;
				}
				else return player.IsInitialized;
			});
		}

		void SeekTo(int seconds) { 
			if (player.IsInitialized)
			{
				player.SeekTo(seconds);
				TimeBinding();
			}
			else {
				player.SetAudioFile("sample.mp3");
				player.SeekTo(seconds);
				TimeBinding();
			}
		}
	}
}
