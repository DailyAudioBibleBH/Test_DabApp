﻿using System;
using System.Collections.Generic;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
	public partial class DabSettingsPage : DabBaseContentPage
	{
		public ViewCell offline { get { return _offline;} }
		public ViewCell reset { get { return _reset;} }
		public ViewCell appInfo { get { return _appInfo;} }
		public ViewCell profile { get { return _profile;} }
		public ViewCell addresses { get { return _addresses;} }
		public ViewCell wallet { get { return _wallet;} }
		public ViewCell donations { get { return _donations;} }
		ViewCell _offline;
		ViewCell _reset;
		ViewCell _appInfo;
		ViewCell _profile;
		ViewCell _addresses;
		ViewCell _wallet;
		ViewCell _donations;

		public DabSettingsPage()
		{
			InitializeComponent();
			DabViewHelper.InitDabForm(this);
			NavigationPage.SetHasBackButton(this, false);
			_offline = Offline;
			_reset = Reset;
			_appInfo = AppInfo;
			_profile = Profile;
			_addresses = Addresses;
			_wallet = Wallet;
			_donations = Donations;
			if (GuestStatus.Current.IsGuestLogin)
			{
				logOut.Clear();
				Listening.Clear();
				Account.Clear();
			}
			//if (Device.Idiom == TargetIdiom.Tablet)
			//{
			//	ControlTemplate NoPlayerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
			//	ControlTemplate = NoPlayerBarTemplate;
			//}

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

		void OnOffline(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabOfflineEpisodeManagementPage());
			}
		}

		void OnReset(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabResetListenedToStatusPage());
			}
		}

		void OnAppInfo(object o, EventArgs e)
		{
			if (Device.Idiom == TargetIdiom.Phone)
			{
				Navigation.PushAsync(new DabAppInfoPage());
			}
		}

		async void OnProfile(object o, EventArgs e) {
			var result = await AuthenticationAPI.GetMember();
			if (Device.Idiom == TargetIdiom.Phone) {
				await Navigation.PushAsync(new DabProfileManagementPage());
			}
		}

		void OnAddresses(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				Navigation.PushAsync(new DabAddressManagementPage());
			}
		}

		async void OnWallet(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				var result = await AuthenticationAPI.GetWallet();
				await Navigation.PushAsync(new DabWalletPage(result));
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
			}
		}

		async void OnDonations(object o, EventArgs e) {
			if (Device.Idiom == TargetIdiom.Phone) {
				ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
				StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
				activity.IsVisible = true;
				activityHolder.IsVisible = true;
				var don = await AuthenticationAPI.GetDonations();
				await Navigation.PushAsync(new DabManageDonationsPage(don));
				activity.IsVisible = false;
				activityHolder.IsVisible = false;
			}
		}
	}
}
