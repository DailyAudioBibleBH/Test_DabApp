using DABApp.DabSockets;
using DABApp.Interfaces;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Version.Plugin;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabCheckEmailPage : DabBaseContentPage
    {
        static bool _fromPlayer;
        static bool _fromDonation;
        int TapNumber = 0;
        private double _width;
        private double _height;
        bool GraphQlLoginRequestInProgress = false;
        bool GraphQlLoginComplete = false;
        SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors
        DabGraphQlVariables variables = new DabGraphQlVariables();

        public DabCheckEmailPage(bool fromPlayer = false, bool fromDonation = false)
        {
            InitializeComponent();
            _width = this.Width;
            _height = this.Height;
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Logo.WidthRequest = GlobalResources.Instance.ScreenSize < 1000 ? 300 : 400;
            }
            NavigationPage.SetHasNavigationBar(this, false);
            _fromPlayer = fromPlayer;
            _fromDonation = fromDonation;
            GlobalResources.LogInPageExists = true;
            ToolbarItems.Clear();
            var email = GlobalResources.GetUserEmail();
            if (email != "Guest" && !String.IsNullOrEmpty(email))
            {
                Email.Text = email;
            }
            if (Device.Idiom == TargetIdiom.Phone)
            {
                Logo.WidthRequest = 250;
                Logo.Aspect = Aspect.AspectFit;
            }
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Container.Padding = 100;
            }

            lblVersion.Text = $"v {CrossVersion.Current.Version}";
        }


        async void OnNext(object o, EventArgs e)
        {
            if (DabSyncService.Instance.IsConnected)
            {
                GlobalResources.WaitStart();
                var ql = await GraphQlFunctions.CheckEmail(Email.Text.Trim());
                GlobalResources.WaitStop();
                if (ql.Success)
                {
                    if (ql.Data.payload.data.checkEmail == true)
                    {
                        //existing user - log them in
                        await Navigation.PushAsync(new DabLoginPage(Email.Text));
                    }
                    else
                    {
                        //new user - register them
                        await Navigation.PushAsync(new DabSignUpPage(Email.Text));
                    }
                }
            }
            else
            {
                //Connect
                DabSyncService.Instance.ConnectWebsocket();
                var ql = await GraphQlFunctions.InitializeConnection(GlobalResources.APIKey);
                await DisplayAlert("Please try again", "We've had to reset your connection to the Daily Audio Bible Servers. Please click Next again to continue", "OK");
            }

        }
        async void OnGuestLogin(object o, EventArgs e)
        {
            //log the user in as a guest
            AuthenticationAPI.LoginGuest();

            //open up the channels page
            NavigationPage _nav = new NavigationPage(new DabChannelsPage());
            _nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
            Application.Current.MainPage = _nav;
            await Navigation.PopToRootAsync();
        }

        public modeData VersionCompare(List<Versions> versions, out modeData mode)
        {
            try
            {
                //Device version
                IAppVersionName service = DependencyService.Get<IAppVersionName>();
                VersionInfo versionInfo = new VersionInfo(service.GetVersionName(), Device.RuntimePlatform);
                IEnumerable<Versions> matchingVersions = versions.Where(x => x.platform.ToUpper() == versionInfo.platform.ToUpper()).ToList(); //Filters to matching platform
                matchingVersions = matchingVersions.Where(x => new System.Version(x.version).CompareTo(versionInfo.versionName) >= 0).ToList(); //Filters to versions at or above curent version
                matchingVersions = matchingVersions.OrderBy(x => x.version).ToList(); //Sorts by version # so we can get the lowest one above or at current
                Versions match = matchingVersions.FirstOrDefault(); //Get the first version out of the filtered / sorted list
                                                                    //Return the match if one was found.
                if (match != null)
                {
                    mode = match.mode;
                    return mode;
                }
                else
                {
                    mode = null;
                    return null;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                mode = null;
                return null;
                throw;
            }

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (GlobalResources.playerPodcast.IsPlaying)
            {
                //Stop the podcast player before continuing
                GlobalResources.playerPodcast.Stop();
            }
            TapNumber = 0;

            //Take action based on the mode of the app
            modeData mode;
            List<Versions> versions = ContentConfig.Instance.versions;

            VersionCompare(versions, out mode);
            if (mode != null)
            {
                string modeResponseCode = "";
                switch (mode.buttons.Count)
                {
                    case 0:
                        //Should not happen
                        modeResponseCode = "";
                        break;
                    case 1:
                        //One button - just tell them something and get the single button's response
                        var mr1 = Application.Current.MainPage.DisplayAlert(mode.title, mode.content, mode.buttons.First().value);
                        mr1.ContinueWith((t1) =>
                        {
                            modeResponseCode = mode.buttons.First().key;
                            HandleModeResponse(modeResponseCode);
                        });
                        break;
                    case 2:
                        //Use display alert with cancel and ok buttons
                        var mr2 = Application.Current.MainPage.DisplayAlert(mode.title, mode.content, mode.buttons.Last().value, mode.buttons.First().value); //Accept - last, Cancel = first
                        mr2.ContinueWith((t1) =>
                        {
                            switch (t1.Result)
                            {
                                case true:
                                    modeResponseCode = mode.buttons.Last().key;
                                    break;
                                case false:
                                    modeResponseCode = mode.buttons.First().key;
                                    break;
                            }
                            HandleModeResponse(modeResponseCode);
                        });

                        break;
                    default:
                        var mr3 = Application.Current.MainPage.DisplayActionSheet(mode.title + "\n\n" + mode.content, null, null, mode.buttons.Select(x => x.value).ToArray());
                        mr3.ContinueWith((t1) =>
                        {
                            modeResponseCode = mode.buttons.First(x => x.value == mr3.Result).key;
                            HandleModeResponse(modeResponseCode);
                        });
                        break;
                }
            }
        }


        private void HandleModeResponse(string modeResponseCode)
        //Handle the user's response to the maintenance mode prompt 
        {
            switch (modeResponseCode)
            {
                case "update": //update app
                               //Open up a page to update the app.
                    var url = string.Empty;
                    var appId = string.Empty;
                    if (Device.RuntimePlatform == "iOS") //Apple
                    {
                        appId = "1215838266"; //TODO: Verify this is the right code
                        url = $"itms-apps://itunes.apple.com/app/id{appId}";
                    }
                    else //Android
                    {
                        //TODO: Test this
                        appId = "dailyaudiobible.dabapp"; //TODO: Verify this is the right code
                        url = $"https://play.google.com/store/apps/details?id={appId}";
                    }

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        //Does not do anything on iOS Debugger.
                        Device.OpenUri(new Uri(url));
                    });
                    DisableAllInputs("Restart app after updating.");


                    break;
                case "guest": //login as guest
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        OnGuestLogin(null, null);
                    });
                    break;
                case "ok": //ok button signifies it's currently offline
                           //Disable inputs
                    DisableAllInputs("Shutdown app and try again later.");
                    break;


            }
        }

        private void DisableAllInputs(string MainButtonText)
        {
            //Disables all inputs and changes the text of the main button
            Device.BeginInvokeOnMainThread(() =>
            {
                Email.IsEnabled = false;
                NextButton.IsEnabled = false;
                btnGuest.IsEnabled = false;
                NextButton.Text = MainButtonText;
            }
            );
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Email.IsEnabled = true;
        }

        async void OnTest(object sender, EventArgs e)
        {
            TapNumber++;
            if (TapNumber >= 5)
            {
                var testprod = GlobalResources.TestMode ? "production" : "test";
                var accept = await DisplayAlert($"Do you want to switch to {testprod} mode?", "You will have to restart the app after selecting \"Yes\"", "Yes", "No");
                if (accept)
                {
                    await adb.ExecuteAsync("DELETE FROM dbSettings");
                    GlobalResources.TestMode = !GlobalResources.TestMode;
                    AuthenticationAPI.SetTestMode();
                    await DisplayAlert($"Switching to {testprod} mode.", $"Please restart the app after receiving this message to fully go into {testprod} mode.", "OK");
                    DisableAllInputs("Shutdown and restart app");
                }
            }
        }
    }
}
