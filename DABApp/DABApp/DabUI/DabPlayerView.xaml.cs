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

		void OnForward30(object o, EventArgs e) {
			AudioPlayer.Instance.Player.Skip(5);
		}

		//void OnSeek(object o, ValueChangedEventArgs e) {
		//	AudioPlayer.Instance.Player.Pause();
		//	AudioPlayer.Instance.Player.SeekTo(Convert.ToInt32(e.NewValue));
		//}

		//void TimeBinding() { 
		//	Device.StartTimer(new TimeSpan(0, 0, 0, 1, 0), () =>
		//	{
		//		if (player.IsInitialized)
		//		{
		//			SeekBar.Maximum = player.TotalTime;
		//			CurrentTime.Text = player.CurrentTime.ToString("##.##");
		//			SeekBar.Value = player.CurrentTime;
		//			//var remaining = TimeSpan.FromMilliseconds(player.RemainingTime());
		//			RemainingTime.Text = player.RemainingTime.ToString("##.##");
		//			//string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds);
		//			return player.IsPlaying;
		//		}
		//		else return player.IsInitialized;
		//	});
		//}

		void SeekTo(int seconds) { 
			if (AudioPlayer.Instance.Player.IsInitialized)
			{
				bool didPause = false;
				if (AudioPlayer.Instance.Player.IsPlaying)
				{
					AudioPlayer.Instance.Player.Pause();
					didPause = true;
				}
				Debug.WriteLine("Seeking {0}", seconds.ToString());
				AudioPlayer.Instance.Player.SeekTo(seconds);
			
				if (didPause)
				{
					AudioPlayer.Instance.Player.Play();
				}
			}
			else {
				AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				//AudioPlayer.Instance.Player.SetAudioFile("sample.mp3");
				AudioPlayer.Instance.Player.SeekTo(seconds);
				//TimeBinding();
			}
		}

		void OnAudioOutput(object o, EventArgs e) {
			DisplayActionSheet(null, "Cancel", null, null);
		}
	}
}
