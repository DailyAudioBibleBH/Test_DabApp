﻿using System;
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
			Stopwatch stopwatch = new Stopwatch();
			TimeBinding();
		}

		void OnPlay(object o, EventArgs e) {
			if (player.IsInitialized())
			{
				if (player.IsPlaying())
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

		void TimeBinding() { 
			Device.StartTimer(new TimeSpan(0, 0, 0, 1, 0), () =>
			{
				if (player.IsInitialized())
				{
					CurrentTime.Text = player.CurrentTime().ToString("##.##");
					//var remaining = TimeSpan.FromMilliseconds(player.RemainingTime());
					RemainingTime.Text = player.RemainingTime().ToString("##.##");//string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds);
					return player.IsPlaying();
				}
				else return player.IsInitialized();
			});
		}
	}
}
