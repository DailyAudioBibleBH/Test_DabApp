﻿using System;
using System.Collections.Generic;
using DABApp.DabAudio;
using SlideOverKit;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabSettingsPage : DabBaseContentPage
    {
        private DabPlayer player = GlobalResources.playerPodcast;
        public ViewCell offline { get { return _offline; } }
        //public ViewCell reset { get { return _reset;} }
        public ViewCell appInfo { get { return _appInfo; } }
        public ViewCell profile { get { return _profile; } }
        public ViewCell addresses { get { return _addresses; } }
        public ViewCell wallet { get { return _wallet; } }
        public ViewCell donations { get { return _donations; } }
        ViewCell _offline;
        //ViewCell _reset;
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
            //_reset = Reset;
            _appInfo = AppInfo;
            _profile = Profile;
            _addresses = Addresses;
            _wallet = Wallet;
            _donations = Donations;
            if (GuestStatus.Current.IsGuestLogin)
            {
                logOut.Title = null;
                logOut.Clear();
                Listening.Title = null;
                Listening.Clear();
                Account.Title = null;
                Account.Clear();
            }
            //if (Device.Idiom == TargetIdiom.Tablet)
            //{
            //	ControlTemplate NoPlayerBarTemplate = (ControlTemplate)Application.Current.Resources["PlayerPageTemplateWithoutScrolling"];
            //	ControlTemplate = NoPlayerBarTemplate;
            //}

        }

        public async void OnForceLogout()
        {
            Application.Current.Properties["IsForcefulLogout"] = "true";
            LogOut.IsEnabled = false;
            await AuthenticationAPI.LogOut();
            var nav = new NavigationPage(new DabLoginPage());
            nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
            Application.Current.MainPage = nav;
            await Navigation.PopToRootAsync();
        }

        public async void OnLogOut(object o, EventArgs e)
        {
            LogOut.IsEnabled = false;
            await AuthenticationAPI.LogOut();
            player.Stop();
            var nav = new NavigationPage(new DabLoginPage());
            nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
            Application.Current.MainPage = nav;
            await Navigation.PopToRootAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LogOut.IsEnabled = true;
        }

        void OnOffline(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                Navigation.PushAsync(new DabOfflineEpisodeManagementPage());
            }
        }

        void OnReset(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                Navigation.PushAsync(new DabResetListenedToStatusPage());
            }
        }

        void OnAppInfo(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                Navigation.PushAsync(new DabAppInfoPage());
            }
        }

        async void OnProfile(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                GlobalResources.WaitStart();
                var result = await AuthenticationAPI.GetMember();
                await Navigation.PushAsync(new DabProfileManagementPage());
                GlobalResources.WaitStop();
            }
            
        }

        void OnAddresses(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                Navigation.PushAsync(new DabAddressManagementPage());
            }
        }

        async void OnWallet(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                GlobalResources.WaitStart();
                var result = await AuthenticationAPI.GetWallet();
                if (result != null)
                {
                    await Navigation.PushAsync(new DabWalletPage(result));
                }
                else
                {
                    await DisplayAlert("Unable to retrieve Wallet information", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
                }
                GlobalResources.WaitStop();
            }
        }

        async void OnDonations(object o, EventArgs e)
        {
            if (GlobalResources.ShouldUseSplitScreen == false)
            {
                GlobalResources.WaitStart();
                var don = await AuthenticationAPI.GetDonations();
                if (don != null)
                {
                    await Navigation.PushAsync(new DabManageDonationsPage(don));
                }
                else await DisplayAlert("Unable to get Donation information.", "This may be due to a loss of internet connectivity.  Please check your connection and try again.", "OK");
                GlobalResources.WaitStop();
            }
        }
    }
}
