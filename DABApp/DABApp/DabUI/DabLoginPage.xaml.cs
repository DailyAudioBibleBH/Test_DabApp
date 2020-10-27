using DABApp.Service;
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
using System.Threading;
using Version.Plugin;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace DABApp
{
    public partial class DabLoginPage : DabBaseContentPage
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
        private string text;

        public DabLoginPage(bool fromPlayer = false, bool fromDonation = false)
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
            var email = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Email;
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

        public DabLoginPage(string emailInput, bool fromPlayer = false, bool fromDonation = false)
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
            var email = adb.Table<dbUserData>().FirstOrDefaultAsync().Result.Email;
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

            Email.Text = emailInput;

            lblVersion.Text = $"v {CrossVersion.Current.Version}";
        }

        async void OnLogin(object o, EventArgs e)
        {
            //log the user in and push up channels page upon success.

            try
            {
                //log the user in 
                GlobalResources.WaitStart("Checking your credentials...");
                var result = await Service.DabService.LoginUser(Email.Text.Trim(), Password.Text);
                if (result.Success == false) throw new Exception(result.ErrorMessage);

                //process the data we got back.
                GraphQlLoginUser user = result.Data.payload.data.loginUser;

                //token was updated successfully
                var newUserData = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;
                newUserData.Token = user.token;
                newUserData.TokenCreation = DateTime.Now;
                await adb.InsertOrReplaceAsync(newUserData);
                
                //re-establish service connection as the user
                await DabService.TerminateConnection();
                result = await DabService.InitializeConnection(newUserData.Token);
                if (result.Success == false) throw new Exception(result.ErrorMessage);

                //perform post-login functions
                await DabServiceRoutines.RunConnectionEstablishedRoutines();

                //push up the channels page
                DabChannelsPage _nav = new DabChannelsPage();
                _nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                //Application.Current.MainPage = _nav;
                await Navigation.PushAsync(_nav);

                //Delete nav stack so user cant back into login screen
                var existingPages = Navigation.NavigationStack.ToList();
                foreach (var page in existingPages)
                {
                    Navigation.RemovePage(page);
                }
            }
            catch (Exception ex)
            {
                GlobalResources.WaitStop();
                await DisplayAlert("Login Failed", $"Your login failed. Please try again.\n\nError Message: {ex.Message} If problem presists please restart your app.", "OK");
                var current = Connectivity.NetworkAccess;

                if (current == NetworkAccess.Internet)
                {
                    Debug.WriteLine("Gained internet access");
                    DabServiceEvents.TrafficOccured(GraphQlTrafficDirection.Connected, "connected");
                    // Connection to internet is available
                    // If websocket is not connected, reconnect
                    if (!DabService.IsConnected)
                    {
                        //reconnect to service
                        var ql = await DabService.InitializeConnection();

                        if (ql.Success)
                        {
                            //perform post-connection operations with service
                            await DabServiceRoutines.RunConnectionEstablishedRoutines();
                        }
                    }
                }
            }


        }

        async void OnForgot(object o, EventArgs e)
        {
            bool answer = await DisplayAlert("Forgot Password?", "Would you like for us to send you an email to reset your password?", "Yes", "No");
            var email = Email.Text;
            if (answer == true)
            {
                var ql = await DabService.ResetPassword(email);
                if (ql.Success)
                {
                    await DisplayAlert("Email Sent", "Please check your email for a link to reset your password.", "OK");
                }
            }
        }

        void OnBack(object o, EventArgs e)
        {
            BackButton.IsEnabled = false;
            Navigation.PopAsync();
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
                Password.IsEnabled = false;
                //GuestLogin.IsEnabled = false;
                Login.IsEnabled = false;
                btnForgot.IsEnabled = false;
                //SignUp.IsEnabled = false;
                Login.Text = MainButtonText;
            }
            );
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Login.IsEnabled = true;
            //GuestLogin.IsEnabled = true;
        }

        void OnCompleted(object sender, System.EventArgs e)
        {
            Password.Focus();
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
                    await adb.ExecuteAsync("DELETE FROM UserData");
                    await adb.ExecuteAsync("DELETE FROM dbSettings");
                    GlobalResources.TestMode = !GlobalResources.TestMode;
                    AuthenticationAPI.SetTestMode();
                    await DisplayAlert($"Switching to {testprod} mode.", $"Please restart the app after receiving this message to fully go into {testprod} mode.", "OK");
                    Login.IsEnabled = false;
                    //GuestLogin.IsEnabled = false;
                    //SignUp.IsEnabled = false;
                }
            }
        }
    }
}
