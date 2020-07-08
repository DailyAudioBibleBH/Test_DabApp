using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DABApp.Service;
using Xamarin.Forms;

namespace DABApp.DabUI
{
    public partial class DabServiceConnect : ContentPage
    {

        ContentAPI contentAPI = new ContentAPI();
        ContentConfig contentConfig = new ContentConfig();
        bool rotateImage = true;

        public DabServiceConnect()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            RotateIconContinuously(); //start rotation
            WaitContent.FadeTo(1, 250); //fade it in

            if (ContentAPI.CheckContent()) //Check for valid content API
            {
                //Determine if the user was logged in at last use
                string token = dbSettings.GetSetting("Token", "");

                //check for version list for required upgrade
                List<Versions> versionList = new List<Versions>();
                versionList = contentConfig.versions;
                contentAPI.GetModes();

                if (versionList != null)
                {
                    //version list in place, force user to log in
                    Application.Current.MainPage = new NavigationPage(new DabCheckEmailPage());
                }
                else if (token == "")
                {
                    //user last logged in as a guest, take them to enter their email
                    Application.Current.MainPage = new NavigationPage(new DabCheckEmailPage());
                }
                else
                {
                    //attempt to connect to service
                    var ql=  await DabService.InitializeConnection(token);
                    if (ql.Success == false && ql.ErrorMessage == "xxx") //TODO: Replace this text with error messgae for invalid token
                    {
                        //token is validated as expired - make them log back in
                        await DabService.TerminateConnection();
                        await DabService.InitializeConnection(GlobalResources.APIKey);
                        Application.Current.MainPage = new NavigationPage(new DabCheckEmailPage());

                    } else
                    {
                        //perform post-login functions
                        await DabServiceRoutines.RunConnectionEstablishedRoutines();

                        Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
                    }
                }
            }
            else
            {
                Application.Current.MainPage = new DabNetworkUnavailablePage(); //Take to network unavailable page if not logged in.
            }

            Application.Current.MainPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
            rotateImage = false;
        }

        async Task RotateIconContinuously()
        {
            while (rotateImage)
            {
                for (int i = 1; i < 7; i++)
                {
                    if (AppIcon.Rotation >= 360f) AppIcon.Rotation = 0;
                    await AppIcon.RotateTo(i * (360 / 6), 1000, Easing.Linear);
                }
            }
        }
    }
}
