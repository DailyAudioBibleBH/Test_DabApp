using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DABApp.Service;
using Xamarin.Essentials;
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
                    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    {
                        //attempt to connect to service
                        var ql = await DabService.InitializeConnection(token);
                        if (ql.Success == false && (ql.ErrorMessage == "Not authenticated as user." || ql.ErrorMessage == "Not authorized."))
                        {
                            var qll = await DabService.UpdateToken();
                            if (qll.Success)
                            {
                                //token was updated successfully
                                dbSettings.StoreSetting("Token", ql.Data.payload.data.updateToken.token);
                                dbSettings.StoreSetting("TokenCreation", DateTime.Now.ToString());

                                //reset the connection using the new token
                                await DabService.TerminateConnection();
                                await DabService.InitializeConnection();
                            }
                            Application.Current.MainPage = new NavigationPage(new DabCheckEmailPage());
                        }
                        else
                        {
                            //perform post-login functions
                            await DabServiceRoutines.RunConnectionEstablishedRoutines();

                            var _nav = new DabChannelsPage();
                            _nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
                            Application.Current.MainPage = new NavigationPage(_nav);
                        }
                    }
                    else
                    {
                        Application.Current.MainPage = new NavigationPage(new DabCheckEmailPage());
                    }
                    
                }
            }
            else
            {
                Application.Current.MainPage = new DabNetworkUnavailablePage(); //Take to network unavailable page if not logged in.
            }

            rotateImage = false;
        }

        async Task RotateIconContinuously()
        {
            int steps = 1;

            while (rotateImage)
            {
                for (int i = 1; i < steps+1 ; i++)
                {
                    if (AppIcon.Rotation >= 360f) AppIcon.Rotation = 0;
                    await AppIcon.RotateTo(i * (360 / steps), 1500, Easing.Linear);
                } 
            }
        }
    }
}
