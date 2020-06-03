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
            //SignUp.IsSelectable = false;
            //var tapper = new TapGestureRecognizer();
            //tapper.NumberOfTapsRequired = 1;


            //tapper.Tapped += (sender, e) =>
            //{
            //    Navigation.PushAsync(new DabSignUpPage("", _fromPlayer, _fromDonation));
            //};
            //SignUp.GestureRecognizers.Add(tapper);
            //SignUp.Text = "<div style='font-size:15px;'>Don't have an account? <font color='#ff0000'>Sign Up</font></div>";
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Container.Padding = 100;
            }

            DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;
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
            //SignUp.IsSelectable = false;
            //var tapper = new TapGestureRecognizer();
            //tapper.NumberOfTapsRequired = 1;


            //tapper.Tapped += (sender, e) =>
            //{
            //    Navigation.PushAsync(new DabSignUpPage("", _fromPlayer, _fromDonation));
            //};
            //SignUp.GestureRecognizers.Add(tapper);
            //SignUp.Text = "<div style='font-size:15px;'>Don't have an account? <font color='#ff0000'>Sign Up</font></div>";
            if (Device.Idiom == TargetIdiom.Tablet)
            {
                Container.Padding = 100;
            }

            Email.Text = emailInput;

            DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;
            lblVersion.Text = $"v {CrossVersion.Current.Version}";
        }

        private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {



            if (GraphQlLoginComplete)
            {
                return; //get out of here once login is complete;
            }

            Device.InvokeOnMainThreadAsync(async () =>
            {

                if (DabSyncService.Instance.IsConnected)
                {
                    SQLiteAsyncConnection adb = DabData.AsyncDatabase;

                    //Message received from the Graph QL - deal with those related to login messages!
                    try
                    {
                        var root = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);
                        //if (root?.payload?.data?.loginUser != null)
                        //{

                        //    //Store the token
                        //    dbSettings sToken = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                        //    if (sToken == null)
                        //    {
                        //        sToken = new dbSettings() { Key = "Token" };
                        //    }
                        //    sToken.Value = root.payload.data.loginUser.token;
                        //    await adb.InsertOrReplaceAsync(sToken);

                        //    //Update Token Life
                        //    ContentConfig.Instance.options.token_life = 5;
                        //    dbSettings sTokenCreationDate = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                        //    if (sTokenCreationDate == null)
                        //    {
                        //        sTokenCreationDate = new dbSettings() { Key = "TokenCreation" };
                        //    }
                        //    sTokenCreationDate.Value = DateTime.Now.ToString();
                        //    await adb.InsertOrReplaceAsync(sTokenCreationDate);
                        //    DabSyncService.Instance.Disconnect(false);
                        //    DabSyncService.Instance.Connect();
                        //    //Reset the connection with the new token
                        //    DabSyncService.Instance.PrepConnectionWithTokenAndOrigin(sToken.Value);

                        //    //Send a request for updated user data
                        //    string jUser = $"query {{user{{wpId,firstName,lastName,email}}}}";
                        //    var pLogin = new DabGraphQlPayload(jUser, new DabGraphQlVariables());
                        //    DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", pLogin)));

                        //}
                        if (root?.payload?.data?.user != null)
                        {
                            //We got back user data!
                            GraphQlLoginComplete = true; //stop processing success messages.
                                                         //Save the data
                            dbSettings sEmail = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
                            dbSettings sFirstName = adb.Table<dbSettings>().Where(x => x.Key == "FirstName").FirstOrDefaultAsync().Result;
                            dbSettings sLastName = adb.Table<dbSettings>().Where(x => x.Key == "LastName").FirstOrDefaultAsync().Result;
                            dbSettings sAvatar = adb.Table<dbSettings>().Where(x => x.Key == "Avatar").FirstOrDefaultAsync().Result;
                            dbSettings sWpId = adb.Table<dbSettings>().Where(x => x.Key == "WpId").FirstOrDefaultAsync().Result;
                            if (sEmail == null) sEmail = new dbSettings() { Key = "Email" };
                            if (sFirstName == null) sFirstName = new dbSettings() { Key = "FirstName" };
                            if (sLastName == null) sLastName = new dbSettings() { Key = "LastName" };
                            if (sAvatar == null) sAvatar = new dbSettings() { Key = "Avatar" };
                            if (sWpId == null) sWpId = new dbSettings() { Key = "WpId" };
                            sEmail.Value = root.payload.data.user.email;
                            sFirstName.Value = root.payload.data.user.firstName;
                            sLastName.Value = root.payload.data.user.lastName;
                            sAvatar.Value = "https://www.gravatar.com/avatar/" + CalculateMD5Hash(GlobalResources.GetUserEmail()) + "?d=mp";
                            sWpId.Value = root.payload.data.user.wpId.ToString();
                            var x = adb.InsertOrReplaceAsync(sEmail).Result;
                            x = adb.InsertOrReplaceAsync(sFirstName).Result;
                            x = adb.InsertOrReplaceAsync(sLastName).Result;
                            x = adb.InsertOrReplaceAsync(sAvatar).Result;
                            x = adb.InsertOrReplaceAsync(sWpId).Result;

                            GraphQlLoginRequestInProgress = false;

                            GuestStatus.Current.IsGuestLogin = false;
                            await AuthenticationAPI.GetMemberData();

                            //user is logged in
                            GlobalResources.Instance.IsLoggedIn = true;
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
                        else if (root?.payload?.data?.resetPassword != null)
                        {
                            if (root.payload.data.resetPassword == true)
                            {
                                await DisplayAlert("Forgot Password?", "Check your email!", "OK");
                            }
                        }

                        else if (root?.payload?.errors?.First() != null)
                        {
                            //if (root?.payload?.errors?.First().message == "Not authorized.")
                            //{
                            //    //Token
                            //    dbSettings s = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
                            //    var test = s.Value;
                            //    if (s != null) await adb.DeleteAsync(s);

                            //    //TokenCreation
                            //    s = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
                            //    if (s != null) await adb.DeleteAsync(s);

                            //    DabGraphQlVariables variables = new DabGraphQlVariables();
                            //    var exchangeTokenQuery = "mutation { updateToken(version: 1) { token } }";
                            //    var exchangeTokenPayload = new DabGraphQlPayload(exchangeTokenQuery, variables);
                            //    var tokenJsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", exchangeTokenPayload));
                            //    DabSyncService.Instance.Send(tokenJsonIn);
                            //    GlobalResources.WaitStop();
                            //    Device.BeginInvokeOnMainThread(() => { DisplayAlert("Token Error", "We're updating your session token. Please try signing up again.", "OK"); ; });
                            //}

                            //else
                            //{
                                GlobalResources.WaitStop();
                                //We have a login error!
                                await DisplayAlert("Login Error", root.payload.errors.First().message, "OK");
                                GraphQlLoginRequestInProgress = false;
                            //}
                        }
                        else
                        {
                            //Some other GraphQL message we don't care about here.

                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        //Some other GraphQL message we don't care about here.

                    }
                }
                else
                {
                    GlobalResources.WaitStop();
                    //DabSyncService.Instance.Init();
                    DabSyncService.Instance.Connect();
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
            if (DabSyncService.Instance.IsConnected)
            {
                try
                {
                    Login.IsEnabled = false;
                    GlobalResources.WaitStart("Checking your credentials...");
                    var result = await AuthenticationAPI.ValidateLogin(Email.Text, Password.Text); //Sends message off to GraphQL
                    if (result == "Request Sent")
                    {
                        //Wait for the reply from GraphQl before proceeding.
                        GraphQlLoginRequestInProgress = true;
                    }

                    else
                    {
                        GlobalResources.WaitStop();
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
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    await DisplayAlert("System Error", "System Error with login. Try again or restart application.", "Ok");
                    Navigation.PushAsync(new DabLoginPage());
                }
            }
            else
            {
                DabSyncService.Instance.Connect();
            }

        }

        async void OnForgot(object o, EventArgs e)
        {
            bool answer = await DisplayAlert("Forgot Password?", "Would you like for us to send you an email to reset your password?", "Yes", "No");
            var email = Email.Text;
            if (answer == true)
            {
                var resetPasswordMutation = $"mutation {{ resetPassword(email: \"{email}\" )}}";
                var resetPasswordPayload = new DabGraphQlPayload(resetPasswordMutation, variables);
                var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", resetPasswordPayload));
                DabSyncService.Instance.Send(JsonIn);
            }
            //var resetPasswordMutation = "mutation { resetPassword(email: \"{Email.Text}\" )}";
            //var resetPasswordPayload = new DabGraphQlPayload(resetPasswordMutation, variables);
            //var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", resetPasswordPayload));
            //DabSyncService.Instance.Send(JsonIn);
            //Navigation.PushAsync(new DabResetPasswordPage());
        }

        void OnBack(object o, EventArgs e)
        {
            BackButton.IsEnabled = false;
            Navigation.PopAsync();
        }

        async void OnGuestLogin(object o, EventArgs e)
        {
            //GuestLogin.IsEnabled = false;
            GlobalResources.WaitStart("Logging you in as a guest...");
            GuestStatus.Current.IsGuestLogin = true;

            dbSettings s = adb.Table<dbSettings>().Where(x => x.Key == "Email").FirstOrDefaultAsync().Result;
            if (s == null) s = new dbSettings() { Key = "Email" };
            s.Value = "Guest";
            await adb.InsertOrReplaceAsync(s);

            //Token
            s = adb.Table<dbSettings>().Where(x => x.Key == "Token").FirstOrDefaultAsync().Result;
            if (s != null) await adb.DeleteAsync(s);

            //TokenCreation
            s = adb.Table<dbSettings>().Where(x => x.Key == "TokenCreation").FirstOrDefaultAsync().Result;
            if (s != null) await adb.DeleteAsync(s);


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
            GlobalResources.WaitStop();
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
