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
			Stopwatch stopwatch = new Stopwatch();
			Device.StartTimer(new TimeSpan(0, 0, 0, 1, 0), () =>
			{
				if (player.IsInitialized())
				{
					CurrentTime.Text = player.CurrentTime().ToString("###.##");
					RemainingTime.Text = player.RemainingTime().ToString("###.##");
					return player.IsPlaying();
				}
				else return player.IsInitialized();
			});
		}

		void OnPlay(object o, EventArgs e) {
			player.Play();
		}
	}
}
