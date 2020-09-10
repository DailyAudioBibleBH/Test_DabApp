using DABApp.Service;
using DABApp.DabSockets;
using DABApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Version.Plugin;
using Xamarin.Forms;
using DABApp.DabUI.BaseUI;

namespace DABApp
{
    public partial class DabCheckEmailPage : DabBaseContentPage
    {
        int TapNumber = 0;
        int ExperimentNumber = 0;
        //Indicator so double connection doesn't get hit from OnResume
        object source = new object();
        private bool hasAppeared; //used to only connect the first time through;

        public DabCheckEmailPage(bool fromPlayer = false, bool fromDonation = false)
        {
            InitializeComponent();

            /* UI Prep
             */
            NavigationPage.SetHasNavigationBar(this, false);
            GlobalResources.LogInPageExists = true;
            ToolbarItems.Clear();
            Email.Text = dbSettings.GetSetting("Email", "");
            lblTestMode.IsVisible = GlobalResources.TestMode;
            if (Device.Idiom == TargetIdiom.Phone)
            {
                Logo.WidthRequest = 250;
                Logo.Aspect = Aspect.AspectFit;
            }
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Container.Padding = 100;
                Logo.WidthRequest = GlobalResources.Instance.ScreenSize < 1000 ? 300 : 400;
            }
            lblVersion.Text = $"v {CrossVersion.Current.Version}";
        }


        async void OnNext(object o, EventArgs e)
        {
            /* Handles when they click next to continue with an email address
             */
            DabUserInteractionEvents.WaitStarted(o, new DabAppEventArgs("Please Wait...", true));
            //check for existing/new email
            var ql = await  DabService.CheckEmail(Email.Text.Trim());
            DabUserInteractionEvents.WaitStopped(o, new EventArgs());

            //determine next path
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
            else
            {
               var reconnect = await DisplayAlert("Error Occured", ql.ErrorMessage, "Try Again","OK");
                if (reconnect == true)
                {
                    DabUserInteractionEvents.WaitStarted(source, new DabAppEventArgs("Retrying...", true));
                    await DabService.InitializeConnection();
                    OnNext(o, e);
                    DabUserInteractionEvents.WaitStopped(o, new EventArgs());
                }

            }

           
        }

        async void OnGuestLogin(object o, EventArgs e)
        {
            //log the user in as a guest
            AuthenticationAPI.LoginGuest();

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

        protected override async void OnAppearing()
        {
            /* Sets up a tap counter for test mode
             * Also sets up forceful version upgrades
             */

            base.OnAppearing();

            if (hasAppeared == false)
            {
                //reset service connection to generic token
                await DabService.TerminateConnection();
                await DabService.InitializeConnection(GlobalResources.APIKey); //connect with generic token
            }
            hasAppeared = true; //don't reset the service connection in the future appearances

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


        async void OnTest(object sender, EventArgs e)
        {
            TapNumber++;
            if (TapNumber >= 5)
            {
                var testprod = GlobalResources.TestMode ? "production" : "test";
                var accept = await DisplayAlert($"Do you want to switch to {testprod} mode?", "You will have to restart the app after selecting \"Yes\"", "Yes", "No");
                if (accept)
                {
                    var adb = DabData.AsyncDatabase;
                    await adb.ExecuteAsync("DELETE FROM dbSettings");
                    GlobalResources.TestMode = !GlobalResources.TestMode;
                    AuthenticationAPI.SetExternalMode(true);
                    await DisplayAlert($"Switching to {testprod} mode.", $"Please restart the app after receiving this message to fully go into {testprod} mode.", "OK");
                    DisableAllInputs("Shutdown and restart app");
                }
            }
        }

        async void OnExperiment(object sender, EventArgs e)
        {
            ExperimentNumber++;
            if (ExperimentNumber >= 2)
            {
                var experimentMode = GlobalResources.ExperimentMode ? "production" : "experiment";
                var accept = await DisplayAlert($"Do you want to switch to {experimentMode} mode?", "This mode will allow you to use new (and unsupported) features of the app. These features may be unstable and may require a deletion and reinstallation of the app if they do not perform as expected. You will have to restart the app after selecting \"Yes\"", "Yes", "No");
                if (accept)
                {
                    var adb = DabData.AsyncDatabase;
                    await adb.ExecuteAsync("DELETE FROM dbSettings");
                    GlobalResources.ExperimentMode = !GlobalResources.ExperimentMode;
                    AuthenticationAPI.SetExternalMode(false);
                    await DisplayAlert($"Switching to {experimentMode} mode.", $"Please restart the app after receiving this message to fully go into {experimentMode} mode.", "OK");
                    DisableAllInputs("Shutdown and restart app");
                }
            }
        }
    }
}
