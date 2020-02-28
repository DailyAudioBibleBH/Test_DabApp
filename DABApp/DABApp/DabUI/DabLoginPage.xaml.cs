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
using Xamarin.Forms;

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
            SignUp.IsSelectable = false;
            var tapper = new TapGestureRecognizer();
            tapper.NumberOfTapsRequired = 1;
            tapper.Tapped += (sender, e) =>
            {
                Navigation.PushAsync(new DabSignUpPage(_fromPlayer, _fromDonation));
            };
            SignUp.GestureRecognizers.Add(tapper);
            SignUp.Text = "<div style='font-size:15px;'>Don't have an account? <font color='#ff0000'>Sign Up</font></div>";
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Container.Padding = 100;
            }


            //MessagingCenter.Subscribe<string>("OptimizationWarning", "OptimizationWarning", (obj) => {
            //    DisplayAlert("Background Playback", "This app needs to disable some battery optimization features to accommodate playback when your device goes to sleep. Please tap 'Yes' on the following prompt to give this permission.", "OK");
            //});

            DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;

        }

        private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            if (GraphQlLoginComplete) return; //get out of here once login is complete;

            Device.InvokeOnMainThreadAsync(async () => {

                SQLiteConnection db = DabData.database;


                //Message received from the Graph QL - deal with those related to login messages!
                try
                {
                    var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);
                    if (root?.payload?.data?.loginUser != null)
                    {

                        //Store the token
                        dbSettings sToken = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Token");
                        if (sToken == null)
                        {
                            sToken = new dbSettings() { Key = "Token" };
                        }
                        sToken.Value = root.payload.data.loginUser.token;
                        db.InsertOrReplace(sToken);



                        //Reset the connection with the new token
                        DabSyncService.Instance.PrepConnectionWithTokenAndOrigin(sToken.Value);

                        //Send a request for updated user data
                        string jUser = $"query {{user{{wpId,firstName,lastName,email}}}}";
                        var pLogin = new DabGraphQlPayload(jUser, new DabGraphQlVariables());
                        DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", pLogin)));

                    }
                    else if (root?.payload?.data?.user != null)
                    {
                        //We got back user data!
                        GraphQlLoginComplete = true; //stop processing success messages.
                        //Save the data
                        dbSettings sEmail = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Email");
                        dbSettings sFirstName = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "FirstName");
                        dbSettings sLastName = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "LastName");
                        dbSettings sAvatar = db.Table<dbSettings>().SingleOrDefault(x => x.Key == "Avatar");
                        if (sEmail == null) sEmail = new dbSettings() { Key = "Email" };
                        if (sFirstName == null) sFirstName = new dbSettings() { Key = "FirstName" };
                        if (sLastName == null) sLastName = new dbSettings() { Key = "LastName" };
                        if (sAvatar == null) sAvatar = new dbSettings() { Key = "Avatar" };
                        sEmail.Value = root.payload.data.user.email;
                        sFirstName.Value = root.payload.data.user.firstName;
                        sLastName.Value = root.payload.data.user.lastName;
                        sAvatar.Value = "https://www.gravatar.com/avatar/" + CalculateMD5Hash(GlobalResources.GetUserEmail()) + "?d=mp";
                        db.InsertOrReplace(sEmail);
                        db.InsertOrReplace(sFirstName);
                        db.InsertOrReplace(sLastName);
                        db.InsertOrReplace(sAvatar);



                        GraphQlLoginRequestInProgress = false;
                        
                        GuestStatus.Current.IsGuestLogin = false;
                        await AuthenticationAPI.GetMemberData();
                        if (_fromPlayer)
                        {
                            await Navigation.PopModalAsync();
                        }
                        else
                        {
                            if (_fromDonation)
                            {
                                var dons = await AuthenticationAPI.GetDonations();
                                if (dons.Length == 1)
                                {
                                    var url = await PlayerFeedAPI.PostDonationAccessToken();
                                    if (url.StartsWith("http"))
                                    {
                                        DependencyService.Get<IRivets>().NavigateTo(url);
                                    }
                                    else
                                    {
                                        await DisplayAlert("Error", "An unknown error occured while logging in. Please try again.", "OK");
                                    }
                                    NavigationPage _nav = new NavigationPage(new DabChannelsPage());
                                    _nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                                    Application.Current.MainPage = _nav;
                                    await Navigation.PopToRootAsync();
                                }
                                else
                                {
                                    NavigationPage _navs = new NavigationPage(new DabManageDonationsPage(dons, true));
                                    _navs.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                                    Application.Current.MainPage = _navs;
                                    await Navigation.PopToRootAsync();
                                }
                            }
                            else
                            {
                                DabChannelsPage _nav = new DabChannelsPage();
                                _nav.SetValue(NavigationPage.BarTextColorProperty, (Color)App.Current.Resources["TextColor"]);
                                //Application.Current.MainPage = _nav;
                                await Navigation.PushAsync(_nav);
                                MessagingCenter.Send<string>("Setup", "Setup");

                                //Delete nav stack so user cant back into login screen
                                var existingPages = Navigation.NavigationStack.ToList();
                                foreach (var page in existingPages)
                                {
                                    Navigation.RemovePage(page);
                                }
                            }
                        }
                    }

                    else if (root?.payload?.errors?.First() != null)
                    {
                        if (GraphQlLoginRequestInProgress == true)
                        {
                            //We have a login error!
                            await DisplayAlert("Login Error", root.payload.errors.First().message, "OK");
                            GraphQlLoginRequestInProgress = false;
                        }
                    }
                    else
                    {
                        //Some other GraphQL message we don't care about here.
                        
                    }
                }
                catch (Exception ex)
                {
                    //Some other GraphQL message we don't care about here.

                }
            });

        }

        public string CalculateMD5Hash(string email)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(email);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }


        async void OnLogin(object o, EventArgs e)
        {
            Login.IsEnabled = false;
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            var result = await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text); //Sends message off to GraphQL
            if (result == "Request Sent")
            {
                //Wait for the reply from GraphQl before proceeding.
                GraphQlLoginRequestInProgress = true;
            }
           
            else
            {
                if (result.Contains("Error"))
                {
                    if (result.Contains("Http"))
                    {
                        await DisplayAlert("Request Timed Out", "There appears to be a temporary problem connecting to the server. Please check your internet connection or try again later.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "An unknown error occured while trying to log in. Please try agian.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Login Failed", result, "OK");
                }
            }
            Login.IsEnabled = true;
            activity.IsVisible = false;
            activityHolder.IsVisible = false;
        }

        void OnForgot(object o, EventArgs e)
        {
            Navigation.PushAsync(new DabResetPasswordPage());
        }

        async void OnGuestLogin(object o, EventArgs e)
        {
            GuestLogin.IsEnabled = false;
            ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
            StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
            activity.IsVisible = true;
            activityHolder.IsVisible = true;
            GuestStatus.Current.IsGuestLogin = true;
            await AuthenticationAPI.ValidateLogin("Guest", "", true);
            if (_fromPlayer)
            {
                await Navigation.PopModalAsync();
            }
            else
            {
                NavigationPage _nav = new NavigationPage(new DabChannelsPage());
                _nav.SetValue(NavigationPage.BarTextColorProperty, Color.FromHex("CBCBCB"));
                Application.Current.MainPage = _nav;
                await Navigation.PopToRootAsync();
            }
            activity.IsVisible = false;
            activity.IsVisible = false;
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
                Password.IsEnabled = false;
                GuestLogin.IsEnabled = false;
                Login.IsEnabled = false;
                btnForgot.IsEnabled = false;
                SignUp.IsEnabled = false;
                Login.Text = MainButtonText;
            }
            );
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Login.IsEnabled = true;
            GuestLogin.IsEnabled = true;
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
                    GlobalResources.TestMode = !GlobalResources.TestMode;
                    AuthenticationAPI.SetTestMode();
                    await DisplayAlert($"Switching to {testprod} mode.", $"Please restart the app after receiving this message to fully go into {testprod} mode.", "OK");
                    Login.IsEnabled = false;
                    GuestLogin.IsEnabled = false;
                    SignUp.IsEnabled = false;
                }
            }
        }
    }
}
