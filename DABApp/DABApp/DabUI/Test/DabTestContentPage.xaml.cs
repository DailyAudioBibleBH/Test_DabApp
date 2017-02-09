using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabTestContentPage: DabBaseContentPage
	{
		public DabTestContentPage()
		{
			InitializeComponent();
		}

		void Handle_Clicked(object sender, System.EventArgs e)
		{
			Navigation.PushAsync(new DabTestContentPage());
		}

		void Handle_LoadAudioFile(object sender, System.EventArgs e)
		{
			AudioPlayer.Instance.Player.SetAudioFile(@"http://dab1.podcast.dailyaudiobible.com/mp3/January03-2017.m4a");
		}
	}
}
