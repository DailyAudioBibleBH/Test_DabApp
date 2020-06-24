using System;
using System.Collections.Generic;
using DABApp.Service;
using Xamarin.Forms;

namespace DABApp.DabUI
{
    public partial class DabServiceConnect : ContentPage
    {

        ContentAPI contentAPI = new ContentAPI();
        ContentConfig contentConfig = new ContentConfig();

        public DabServiceConnect()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

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
                        Application.Current.MainPage = new NavigationPage(new DabChannelsPage());
                    }
                }
            }
            else
            {
                Application.Current.MainPage = new DabNetworkUnavailablePage(); //Take to network unavailable page if not logged in.
            }

            Application.Current.MainPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));

        }
    }
}
