﻿using System;
using DABApp.Droid;
using Xamarin.Forms;
using Android.Media;
using Android.Content.Res;
using System.Collections.Generic;
using System.IO;

[assembly: Dependency(typeof(AudioService))]
namespace DABApp.Droid
{
	public class AudioService: IAudio
	{
		public static MediaPlayer player;
		public static bool IsLoaded;
		public static dbEpisodes Episode;
		public static string FileName;

		public AudioService()
		{
		}

		public void SetAudioFile(string fileName, dbEpisodes episode)
		{
			player = new MediaPlayer();
			player.Prepared += (s, e) =>
			{
				IsLoaded = true;
			};
			player.Completion += (s, e) =>
			{
				IsLoaded = false;
			};
			player.Error += OnError;
			if (fileName.Contains("http://") || fileName.Contains("https://"))
			{
				player.SetDataSource(fileName);
			}
			else
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				fileName = Path.Combine(doc, fileName);
				player.SetDataSource(fileName);
			}
			Episode = episode;
			FileName = fileName;
			player.Prepare();
		}

		public void Play() {
			player.Start();
		}

		public void Pause() {
			player.Pause();
		}

		public void SeekTo(int seconds) {
			player.SeekTo(seconds * 1000);
		}

		public void Skip(int seconds)
		{
			player.SeekTo((Convert.ToInt32(CurrentTime) + seconds) * 1000);
		}

		public void Unload()
		{
			IsLoaded = false;
		}

		public void SwitchOutputs()
		{
			throw new NotImplementedException();
		}

		public bool IsInitialized {
			get { return IsLoaded;}
		}

		public bool IsPlaying {
			get { return player != null ? player.IsPlaying : false;}
		}

		public double CurrentTime {
			get { return player.CurrentPosition > 0 ? player.CurrentPosition/1000: 0;}
		}

		public double RemainingTime {
			get { return (player.CurrentPosition - player.Duration) > 0 ? (player.CurrentPosition - player.Duration) / 1000: 0;}
		}

		public double TotalTime {
			get { return player.Duration >0 ? player.Duration / 1000 : 60; }
		}

		public bool PlayerCanKeepUp
		{
			get
			{
				if (player != null)
				{
					return true;
				}
				else return false;
			}
		}

		void OnError(object o, EventArgs e)
		{
			player.Dispose();
			SetAudioFile(FileName, Episode);
		}
	}
}
