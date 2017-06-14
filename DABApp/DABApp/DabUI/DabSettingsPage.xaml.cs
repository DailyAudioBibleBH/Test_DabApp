using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSettingsPage : DabBaseContentPage
	{
		public NonScrollingListView listening { get { return _listening;} }
		public NonScrollingListView other { get { return _other;} }
		NonScrollingListView _listening;
		NonScrollingListView _other;

		public DabSettingsPage()
		{
			InitializeComponent();
			Listening.ItemsSource = new List<Preset> { new Preset("Offline Episodes", true), new Preset("Reset listened to status", true) };
			Other.ItemsSource = new List<Preset> { new Preset("App info", true) };
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
			if (GuestStatus.Current.IsGuestLogin)
			{
				LogOut.IsVisible = false;
				Listening.IsVisible = false;
			}
			else
			{
				LogOut.IsVisible = true;
				Listening.IsVisible = true;
			}
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				ControlTemplate NoPlayerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
				ControlTemplate = NoPlayerBarTemplate;
			}
			_listening = Listening;
			_other = Other;

		}

		async void OnLogOut(object o, EventArgs e)
		{
			LogOut.IsEnabled = false;
			AudioPlayer.Instance.Pause();
			AudioPlayer.Instance.Unload();
			if (await AuthenticationAPI.LogOut())
			{
				Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			}
			else
			{
				//await DisplayAlert("OH NO!", "Something went wrong, Sorry.", "OK");
				Navigation.PushModalAsync(new NavigationPage(new DabLoginPage()));
			}
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			LogOut.IsEnabled = true;
		}

		void OnListening(object o, ItemTappedEventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone)
			{
				var pre = e.Item as Preset;
				switch (pre.duration)
				{
					case "Offline Episodes":
						Navigation.PushAsync(new DabOfflineEpisodeManagementPage());
						break;
					case "Reset listened to status":
						Navigation.PushAsync(new DabResetListenedToStatusPage());
						break;
				}
			}
		}

		void OnOther(object o, ItemTappedEventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				var pre = e.Item as Preset;
				switch (pre.duration) { 
					case "App info":
						Navigation.PushAsync(new DabAppInfoPage());
						break;
				}
			}
		}
	
	}
}
