using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.Service;
using SQLite;
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

            if (Device.RuntimePlatform == Device.iOS)
            {
                RotateIconContinuously(); //start rotation
                WaitContent.FadeTo(1, 250); //fade it in
            }
            else
            {
                WaitContent.Opacity = 1;
            }
            if (GlobalResources.TestMode)
            {
                lblTestMode.IsVisible = true;
                if (Device.RuntimePlatform == Device.iOS)
                {
                    lblTestMode.FadeTo(1, 500, Easing.BounceIn);
                }
                else
                {
                    lblTestMode.Opacity = 1;
                }
            }
            SQLiteAsyncConnection adb = DabData.AsyncDatabase;
            //Determine if the user was logged in at last use
            //If first launch of app make guest account before running CheckContent
            var user = GlobalResources.Instance.LoggedInUser;
            if (user == null)
            {
                dbUserData firstGuestLogin = new dbUserData();
                firstGuestLogin.Token = "";
                firstGuestLogin.Email = "";
                firstGuestLogin.FirstName = "Guest";
                firstGuestLogin.LastName = "Guest";
                firstGuestLogin.WpId = 0;
                firstGuestLogin.Channel = "";
                firstGuestLogin.Channels = "";
                firstGuestLogin.Id = 0;
                firstGuestLogin.Language = "";
                firstGuestLogin.NickName = "Guest";
                firstGuestLogin.UserRegistered = DateTime.Now;
                firstGuestLogin.TokenCreation = DateTime.Now;
                firstGuestLogin.ActionDate = DateTime.MinValue;
                firstGuestLogin.ProgressDate = DateTime.MinValue;
                firstGuestLogin.IsLoggedIn = false;
                await adb.InsertOrReplaceAsync(firstGuestLogin);
            }

            if (ContentAPI.CheckContent()) //Check for valid content API
            {
                //check for version list for required upgrade
                List<Versions> versionList = new List<Versions>();
                versionList = contentConfig.versions;
                contentAPI.GetModes();

                /*
                 * database operations
                 */

                //cleanup data transfers older than a week.
                try
                {
                    var oldDataTransfersAge = 7;
                    var deleteDate = DateTime.Now.AddDays(-1 * oldDataTransfersAge);
                    var toDelete = adb.Table<dbDataTransfers>().Where(x => x.LogTimestamp <= deleteDate).ToListAsync().Result;
                    int cnt = 0;
                    foreach (var item in toDelete)
                    {
                        await adb.DeleteAsync(toDelete);
                        cnt++;
                    }
                    //var cnt = adb.ExecuteAsync($"delete from dbDataTransfers where LogTimestamp <=\"{deleteDate}\"").Result;
                    Debug.WriteLine($"Removed {cnt} data transfers");
                }
                catch (Exception ex)
                {
                    //ignore errors
                }


                //#if DEBUG
                //                /*
                //                 * Area to do some database operations to prep app for debugging operations,
                //                 * such as database cleaning, setting resets, etc.
                //                 */

                //                //reset the dab episodelastquerydate
                //                var queryDate = new DateTime(2020, 8, 21, 8, 0, 0);
                //                dbSettings.StoreSetting("EpisodeQueryDate227", queryDate.ToUniversalTime().ToString());

                //                ////delete episodes from today
                //                var adb = DabData.AsyncDatabase;
                //                var eps = await adb.Table<dbEpisodes>().Where(x => x.PubDate >= queryDate).ToListAsync();
                //                foreach( var ep in eps)
                //                {
                //                    await adb.DeleteAsync(ep);
                //                }                    


                //#endif

                NavigationPage navPage;
                user = GlobalResources.Instance.LoggedInUser;

                if (versionList != null)
                {
                    //version list in place, force user to log in
                    navPage = new NavigationPage(new DabCheckEmailPage());
                }
                else if (user.Token == "")
                {
                    //user last logged in as a guest, take them to enter their email
                    navPage = new NavigationPage(new DabCheckEmailPage());
                }
                else
                {
                    //user was logged in last time

                    //check for internet
                    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    {
                        //we have internet access - establish a connection with the token
                        var ql = await DabService.InitializeConnection(user.Token);
                        if (ql.Success == false && (ql.ErrorMessage == "Not authenticated as user." || ql.ErrorMessage == "Not authorized")) //TODO: Replace this text with error messgae for invalid token
                        {
                            //token is validated as expired - make them log back in
                            await DabService.TerminateConnection();
                            await DabService.InitializeConnection(GlobalResources.APIKey);
                            navPage = new NavigationPage(new DabCheckEmailPage());
                        }
                        else
                        {
                            //token is good. perform post-login functions and continue on
                            await DabServiceRoutines.RunConnectionEstablishedRoutines();
                            navPage = new NavigationPage(new DabChannelsPage());
                        }
                    }
                    else
                    {
                        //no internet access - proceed on hoping the token is good
                        DabServiceEvents.TrafficOccured(GraphQlTrafficDirection.Disconnected, "no internet");
                        navPage = new NavigationPage(new DabChannelsPage());
                    }
                }
                //proceed on to the appopriate nav page
                navPage.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
                Application.Current.MainPage = navPage;
                return;
            }
            else
            {
                Application.Current.MainPage = new DabNetworkUnavailablePage(); //Take to network unavailable page if not logged in.
            }

            //finish rotating the image
            rotateImage = false;
        }


        async Task RotateIconContinuously()
        {
            int steps = 1;

            while (rotateImage)
            {
                for (int i = 1; i < steps + 1; i++)
                {
                    if (AppIcon.Rotation >= 360f) AppIcon.Rotation = 0;
                    await AppIcon.RotateTo(i * (360 / steps), 1000, Easing.CubicInOut);
                }
            }
        }
    }
}
