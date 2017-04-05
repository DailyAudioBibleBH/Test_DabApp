﻿using System;
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

		public DabPlayerPage(dbEpisodes episode)
		{
			InitializeComponent();

			Episode = episode;
			AudioPlayer.Instance.ShowPlayerBar = false;
			SeekBar.Value = AudioPlayer.Instance.CurrentTime;
			DabViewHelper.InitDabForm(this);
			backgroundImage = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == episode.channel_title).images.backgroundPhone;
			BackgroundImage.Source = backgroundImage;
			BindingContext = episode;
			Date.Text = $"{episode.PubMonth} {episode.PubDay.ToString()} {episode.PubYear.ToString()}";
			HLabel.Text = "<h2>About Us</h2>\n<p>Having scripture in the daily rhythm of your life changes things. For more than 10 years we’ve seen it personally—and in the lives of others. In the amount of time we can easily waste in a day, we go through the entire Bible in a year together. At the end of a year you’ll discover a surprising change. Everything we do revolves around pointing people to Scripture and working to break down walls that inhibit community.</p>\n<h4>Mission Statement</h4>\n<p>The Daily Audio Bible is committed to guiding Christian's worldwide into an intimate and daily friendship with the Bible.  This is accomplished by providing the spoken Word of God freely in multiple languages and by the creation and establishment of communities that have no geographical limitations.  Our efforts are devoted to exposing the rich texture and heritage of the Bible's life changing power by educating the Believer on how to engage in and interact with the ancient Scriptures in a future world.</p>\n<h4>Vision Statement</h4>\n<p>Our ultimate goal is to provide the spoken Word of God to as many people as will listen in every region of the world that God will allow using whatever means God will allow for as long as God will allow. To be people of the Scriptures, fervent in prayer and having a daily and intimate walk with God. To build stable and Christ honoring community as we take our place in the global advancement of the Kingdom of God.</p>\n<h4>Guiding Scripture</h4>\n<p>2 Timothy 3:16-17 (New International Version)<br />\\nAll Scripture is God-breathed and is useful for teaching, rebuking, correcting and training in righteousness, so that the man of God may be thoroughly equipped for every good work.</p>";
		}

		void OnPlay(object o, EventArgs e)
		{
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
				AudioPlayer.Instance.Player.SetAudioFile(Episode.url);
				//GlobalResources.Player.PlayAudioFile("http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.Player.Play();
				AudioPlayer.Instance.PlayButtonText = "Pause";
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
					AudioPlayer.Instance.showPlayerBar = false;
					Listen.IsVisible = true;
					BackgroundImage.IsVisible = true;
					break;
				case 1:
					Listen.IsVisible = false;
					Journal.IsVisible = false;
					AudioPlayer.Instance.showPlayerBar = true;
					Read.IsVisible = true;
					BackgroundImage.IsVisible = false;
					break;
				case 2:
					Read.IsVisible = false;
					Listen.IsVisible = false;
					AudioPlayer.Instance.showPlayerBar = true;
					Journal.IsVisible = true;
					BackgroundImage.IsVisible = false;
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
				//AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
				AudioPlayer.Instance.Player.SetAudioFile("@"+Episode.url);
				AudioPlayer.Instance.Player.Play();
				//ProgressBinding();
				AudioPlayer.Instance.PlayButtonText = "Pause";
			}
		}

		void OnPodcast(object o, EventArgs e)
		{
			SegControl.SelectTab(0);
		}
	}
}
