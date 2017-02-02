﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DABApp
{
	public class AudioPlayer : INotifyPropertyChanged
	{
		// Singleton for use throughout the app
		public static AudioPlayer Instance { get; private set;}
		static AudioPlayer() { Instance = new AudioPlayer();}
		//Don't allow creation of the class elsewhere in the app.
		private AudioPlayer()
		{
		}

		private bool _IsLoaded = true;
		private bool _IsPlaying = false;
		private string _PlayButtonText = "Play";
		private double _Progress = 0;
		private double _CurrentTime = 0;
		private double _RemainingTime = 0;
		private double _TotalTime = 0;

		//property for whether or not a file is loaded (and whether to display the player bar)
		public bool IsLoaded
		{
			get
			{
				return _IsLoaded;
			}
			set
			{
				SetValue(ref _IsLoaded, value);
			}
		}

		//property for whether a file is being played or not
		public bool IsPlaying
		{
			get
			{
				return _IsPlaying;
			}
			set
			{
				_IsPlaying = value;
				OnPropertyChanged("IsPlaying");
			}
		}

		public string PlayButtonText { 
			get {
				return _PlayButtonText;
			}
			set {
				_PlayButtonText = value;
				OnPropertyChanged("PlayButtonText");
			}
		}

		public double Progress { 
			get {
				return _Progress;
			}
			set {
				_Progress = value;
				OnPropertyChanged("Progress");
			}
		}

		public double CurrentTime
		{
			get
			{
				return _CurrentTime;
			}
			set
			{
				_CurrentTime = value;
				OnPropertyChanged("CurrentTime");
			}
		}

		public double RemainingTime
		{
			get
			{
				return _RemainingTime;
			}
			set
			{
				_RemainingTime= value;
				OnPropertyChanged("RemainingTime");
			}
		}

		public double TotalTime
		{
			get
			{
				return _TotalTime;
			}
			set
			{
				_TotalTime = value;
				OnPropertyChanged("TotalTime");
			}
		}
		protected void SetValue<T>(ref T field, T value,
			[CallerMemberName]string propertyName = null)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				OnPropertyChanged(propertyName);
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
