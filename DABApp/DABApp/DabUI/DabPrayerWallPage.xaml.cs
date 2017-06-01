using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace DABApp
{
	public partial class DabPrayerWallPage : DabBaseContentPage
	{
		public DabPrayerWallPage(View view)
		{
			InitializeComponent();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			if (GuestStatus.Current.IsGuestLogin)
			{
				LoginPrayerWall.IsVisible = true;
				PrayerWall.IsVisible = false;
			}
			else {
				LoginPrayerWall.IsVisible = false;
				PrayerWall.IsVisible = true;
			}
		}

		void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			Navigation.PushModalAsync(new DabLoginPage(true));
			Login.IsEnabled = true;
		}
	}
}
