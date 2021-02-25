﻿using System;
using SlideOverKit;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using System.Threading.Tasks;
using Plugin.Connectivity;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using DABApp.Service;
using DABApp.DabUI.BaseUI;
using Xamarin.Essentials;

namespace DABApp
{
    public class DabBaseContentPage : MenuContainerPage
    {
        //public ActivityIndicator activity { get; set;}
        //public StackLayout activityHolder { get; set;}
        bool giving;
        Resource _resource = new Resource();
        //TODO: Create a method or something that pages that inherit from this can receive and do what they need to do:
        //Episode list - reload list like pull down
        //player page - BindCOntrols to episode
        //tablet page - reload list, bind controls

        //Keepalive Indicator
        ToolbarItem keepaliveButton;


        public DabBaseContentPage()
        {
            //Default Page properties
            //this.Padding = new Thickness(10, 10); //Add some padding around all page controls
            if (GlobalResources.TestMode)
            {
                Title = "*** TEST MODE ***";
            }
            else if (GlobalResources.ExperimentMode)
            {
                Title = "*** EXPERIMENTAL MODE ***";
            }
            else
            {
                Title = "DAILY AUDIO BIBLE";
            }
            keepaliveButton = new ToolbarItem();
            //Control template (adds the player bar)
            ControlTemplate playerBarTemplate = (ControlTemplate)Xamarin.Forms.Application.Current.Resources["PlayerPageTemplate"];
            RelativeLayout container = new RelativeLayout();
            ControlTemplate = playerBarTemplate;
            On<Xamarin.Forms.PlatformConfiguration.iOS>().SetUseSafeArea(true);

            //Navigation properties
            Xamarin.Forms.NavigationPage.SetBackButtonTitle(this, "");

            //Keepalive indicator
            DabServiceEvents.TrafficOccuredEvent += DabServiceEvents_TrafficOccuredEvent;

            //Wait indicator
            DabUserInteractionEvents.WaitStartedEvent += DabUserInteractionEvents_WaitStartedEvent;
            DabUserInteractionEvents.WaitStoppedEvent += DabUserInteractionEvents_WaitStoppedEvent;

            //Subscribe to stopping wait ui
            MessagingCenter.Subscribe<string>("dabapp", "Wait_Stop", (obj) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StackLayout activityContent = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityContent");
                    StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
                    activityHolder.IsVisible = false;
                    activityContent.IsVisible = false;
                });
            });

            //Slide Menu
            this.SlideMenu = new DabMenuView();
            if (Device.RuntimePlatform == "iOS")
            {
                //Keepalive Button
                keepaliveButton = new ToolbarItem();
                keepaliveButton.Text = ""; //will be set to an icon when active;
                keepaliveButton.Priority = 1;
                this.ToolbarItems.Add(keepaliveButton);

                //Menu Button
                var menuButton = new ToolbarItem();
                menuButton.SetValue(AutomationProperties.NameProperty, "Menu");
                menuButton.SetValue(AutomationProperties.HelpTextProperty, "Menu");
                menuButton.Text = "Menu";
                menuButton.Priority = 1; //priority 1 causes it to be moved to the left by the platform specific navigation renderer
                menuButton.Icon = "ic_menu_white.png";
                AutomationProperties.SetHelpText(menuButton, "Menu");
                menuButton.Clicked += (sender, e) =>
                {
                    this.ShowMenu();
                };
                this.ToolbarItems.Add(menuButton);



                //Record Button
                var recordButton = new ToolbarItem();
                recordButton.SetValue(AutomationProperties.NameProperty, "Record");
                recordButton.SetValue(AutomationProperties.HelpTextProperty, "Record");
                recordButton.Text = "Record";
                recordButton.Icon = "record_btn.png";
                recordButton.Priority = 0;
                AutomationProperties.SetHelpText(recordButton, "Record");
                recordButton.Clicked += OnRecord;
                this.ToolbarItems.Add(recordButton);

                //Utility button (debug only)
                var utilityButton = new ToolbarItem();
                utilityButton.Text = "UT";
                utilityButton.Priority = 0;
                utilityButton.Clicked += UtilityButton_Clicked;
#if DEBUG
                this.ToolbarItems.Add(utilityButton);
#endif

                //Give button on the right (priority 1)
                var giveButton = new ToolbarItem();
                giveButton.SetValue(AutomationProperties.NameProperty, "Give");
                giveButton.Text = "Give";
                //giveButton.Icon = "ic_attach_money_white.png";
                giveButton.Priority = 0; //default
                giveButton.Clicked += OnGive;
                this.ToolbarItems.Add(giveButton);


            }
            else
            {
                MessagingCenter.Send("Setup", "Setup");
            }
        }

        private async void UtilityButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DabUtilityPage());
        }

        private void DabUserInteractionEvents_WaitStoppedEvent(object source, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                StackLayout activityContent = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityContent");
                StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
                activityHolder.IsVisible = false;
                activityContent.IsVisible = false;
            });
        }

        private void DabUserInteractionEvents_WaitStartedEvent(object source, DabAppEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
                StackLayout activityContent = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityContent");
                ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
                Label activityLabel = ControlTemplateAccess.FindTemplateElementByName<Label>(this, "activityLabel");
                Button activityButton = ControlTemplateAccess.FindTemplateElementByName<Button>(this, "activityButton");
                //Reset the fade if needed.
                if (activityHolder.IsVisible == false)
                {
                    activityHolder.Opacity = 0;
                    activityContent.Opacity = 0;
                    activityHolder.FadeTo(.75, 500, Easing.CubicIn);
                    activityContent.FadeTo(1, 500, Easing.CubicIn);
                }
                activityButton.IsVisible = e.hasCancel;
                activityButton.Clicked += StopWait;
                activityLabel.Text = e.message;
                activity.IsVisible = true;
                activityContent.IsVisible = true;
                activityHolder.IsVisible = true;
            });
        }

        private async void DabServiceEvents_TrafficOccuredEvent(GraphQlTrafficDirection direction, string traffic)
        {
            //display icon in nav bar based on traffic type
            bool hideWhenDone = true;
            switch (direction)
            {

#if DEBUG
                case GraphQlTrafficDirection.Inbound:
                    //inbound traffic
                    keepaliveButton.Text = "↓";
                    break;

                case GraphQlTrafficDirection.Outbound:
                    //outbound traffic
                    keepaliveButton.Text = "↑";
                    break;
                case GraphQlTrafficDirection.Connected:
                    //internet connected
                    keepaliveButton.Text = "👌"; //"◦";
                    hideWhenDone = false;
                    break;
                case GraphQlTrafficDirection.Disconnected:
                    //internet disconnected
                    keepaliveButton.Text = "🚫"; //"◦";
                    hideWhenDone = false;
                    break;
#else
                case GraphQlTrafficDirection.Inbound:
                    //inbound traffic
                    keepaliveButton.Text = "·";
                    break;

                case GraphQlTrafficDirection.Outbound:
                    //outbound traffic
                    keepaliveButton.Text = "·"; 
                    break;
                case GraphQlTrafficDirection.Connected:
                    //internet connected
                    keepaliveButton.Text = "⊙"; //"◦";
                    hideWhenDone = false;
                    break;
                case GraphQlTrafficDirection.Disconnected:
                    //internet disconnected
                    keepaliveButton.Text = "◦"; //"◦";
                    hideWhenDone = false;
                    break;
#endif

            }

            //hide the button after a moment for traffic
            if (hideWhenDone)
            {
                await Task.Delay(100);
                keepaliveButton.Text = "";
            }
        }

        private void StopWait(object sender, EventArgs e)
        {
            //GlobalResources.WaitStop();
            DabUserInteractionEvents.WaitStopped(sender, new EventArgs());
        }

        async void OnGive(object sender, EventArgs e)
        {
            try
            {
                //Send info to Firebase analytics that user tapped an action we track
                var info = new Dictionary<string, string>();
                object source = new object();
                info.Add("title", "give");
                DependencyService.Get<IAnalyticsService>().LogEvent("action_navigation", info);

                if (!giving)
                {
                    DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Connecting to the DAB Server...", true));
                    giving = true;
                    if (GuestStatus.Current.IsGuestLogin)
                    {
                        //guest giving - just take them to the give site
                        DependencyService.Get<IAnalyticsService>().LogEvent("give_guest");
                        if (CrossConnectivity.Current.IsConnected)
                        {
                            try
                            {
                                Device.OpenUri(new Uri(GlobalResources.GiveUrl));
                            }
                            catch (Exception ex)
                            {
                                // An unexpected error occured. No browser may be installed on the device.
                            }
                        }
                        else await DisplayAlert("An Internet connection is needed to give.", "There is a problem with your internet connection that would prevent you from giving. Please check your internet connection and try again.", "OK");
                        //else await DisplayAlert("An Internet connection is needed to log in.", "There is a problem with your internet connection that would prevent you from logging in.  Please check your internet connection and try again.", "OK");
                    }
                    else
                    {
                        DependencyService.Get<IAnalyticsService>().LogEvent("give_user");
                        var num = 15000;
                        var t = AuthenticationAPI.GetActiveDonations();
                        List<dbUserCampaigns> dons = AuthenticationAPI.GetActiveDonations();

                        if (dons != null)
                        {
                            if (dons.Count > 0)
                            {
                                //manage existing donations
                                await Navigation.PushAsync(new DabManageDonationsPage());
                            }
                            else
                            {
                                //no active donations - take them to give page
                                String url = "";
                                var adb = DabData.AsyncDatabase;

                                //find the campaign id to use
                                int CampaignId = 1; //default if nothing else can be found
                                var publishedCampaigns = adb.Table<dbCampaigns>().Where(x => x.campaignStatus == "publish").ToListAsync().Result;
                                if (publishedCampaigns.Count > 0)
                                {
                                    //use the first one to start with
                                    CampaignId = publishedCampaigns.First().campaignId;

                                    //search for default campaign
                                    var defaultCampaign = publishedCampaigns.SingleOrDefault(x => x.@default == true);
                                    if (defaultCampaign != null)
                                    {
                                        CampaignId = defaultCampaign.campaignId;
                                    }    
                                }


                                var ask = PlayerFeedAPI.PostDonationAccessToken(CampaignId);
                                if (ask == await Task.WhenAny(ask, Task.Delay(num)))
                                {
                                    url = await PlayerFeedAPI.PostDonationAccessToken(CampaignId);
                                }
                                else await DisplayAlert("Request Timeout exceeded for posting Donation Access Token.", "This may be a server or internet connectivity issue.", "OK");
                                if (url.StartsWith("http"))
                                {
                                    Device.OpenUri(new Uri(url));
                                }
                                else
                                {
                                    Device.OpenUri(new Uri(GlobalResources.GiveUrl)); //default give location
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Device.OpenUri(new Uri(GlobalResources.GiveUrl));
                            }
                            catch (Exception ex)
                            {
                                // An unexpected error occured. No browser may be installed on the device.
                            }
                        }
                    }
                    //GlobalResources.WaitStop();
                    DabUserInteractionEvents.WaitStopped(sender, new EventArgs());
                    giving = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        //TODO: Create a method or something that pages that inherit from this can receive and do what they need to do:
        //Episode list - reload list like pull down
        //player page - BindCOntrols to episode
        //tablet page - reload list, bind controls

        public static void UpdatePlayerEpisodeData()
        {
            MessagingCenter.Send<string>("Refresh", "Refresh");

        }

        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<string>("Menu", "Menu");
            MessagingCenter.Unsubscribe<string>("Give", "Give");
            MessagingCenter.Unsubscribe<string>("Record", "Record");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Unsubscribe();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Device.RuntimePlatform == "Android")
            {
                MessagingCenter.Subscribe<string>("Menu", "Menu", (sender) =>
                {
                    if (Navigation.NavigationStack.Count() > 0 && Navigation.NavigationStack.Last() == this)
                    {
                        this.ShowMenu();
                    }
                });
                MessagingCenter.Subscribe<string>("Give", "Give", (sender) => { OnGive(sender, new EventArgs()); });
                MessagingCenter.Subscribe<string>("Record", "Record", (sender) => { OnRecord(sender, new EventArgs()); });
            }
        }


        async void OnRecord(object o, EventArgs e)
        {
            GlobalResources.GoToRecordingPage();
        }
    }
}