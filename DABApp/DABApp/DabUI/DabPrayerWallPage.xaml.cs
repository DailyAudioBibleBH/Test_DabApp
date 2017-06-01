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
				Login.IsVisible = true;
				Post.IsVisible = false;
			}
			else {
				Login.IsVisible = false;
				Post.IsVisible = true;
			}
		}

		void OnLogin(object o, EventArgs e) {
			Login.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			Navigation.PushModalAsync(new DabLoginPage(true));
			Login.IsEnabled = true;
		}

		void OnPost(object o, EventArgs e) { 
			
		}
	}
}
