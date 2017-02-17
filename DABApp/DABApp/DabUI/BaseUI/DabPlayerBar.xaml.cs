﻿using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPlayerBar : ContentView
	{
		public DabPlayerBar()
		{
			InitializeComponent();
		}

		//Play - Pause
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

		//Show Player Page
		void OnShowPlayer(object o, EventArgs e)
		{
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			page.PushAsync(new DabPlayerPage());

		}

		//Show share dialog
		void OnShare(object o, EventArgs e)
		{
			NavigationPage page = (NavigationPage)Application.Current.MainPage;
			page.DisplayAlert("Share episode", "This button will share this episode.", "OK");
		}
	}
}