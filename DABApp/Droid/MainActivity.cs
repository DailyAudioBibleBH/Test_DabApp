using System;

using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using SegmentedControl.FormsPlugin.Android;
using Android.Util;
using Android.Support.V7;
using Xamarin.Forms;
using SQLite;
using Xamarin.Forms.Platform.Android;
using Android.Graphics;
using Android.Support.V4.Media.Session;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Android.Support.Design.Widget;
using Android.Content.Res;
using Android.Views.Accessibility;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using Android.Telephony;
using DABApp.Droid.DependencyServices;
using Android.Media;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Device = Xamarin.Forms.Device;
using ImageButton = Android.Widget.ImageButton;
using DABApp.DabAudio;
using Plugin.CurrentActivity;
using Xamarin.Forms.Platform.Android.AppLinks;

namespace DABApp.Droid
{
    //Deep Linking, allowing outside links to app
    [IntentFilter(new[] { Intent.ActionView},
        Categories = new[]
        {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "http", DataPathPrefix = "dabapp://", DataHost = "https://dailyaudiobible.com/", AutoVerify = true )]

    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[]
        {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "https", DataPathPrefix = "dabapp://", DataHost = "https://dailyaudiobible.com/", AutoVerify = true)]

    [Activity(Label = "DABApp.Droid", Icon = "@drawable/app_icon", Theme = "@style/MyTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.FullUser)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, DataScheme = "dab", Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable })]

    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity //, AudioManager.IOnAudioFocusChangeListener
    {
        CallReceiver callReceiver;

        protected override void OnCreate(Bundle bundle)
        {
            SQLitePCL.Batteries.Init(); //Setting up the SQLite database to be Serialized prevents a lot of errors when using the database so regularly.
            SQLitePCL.raw.sqlite3_shutdown();
            SQLitePCL.raw.sqlite3_config(Convert.ToInt32(SQLite3.ConfigOption.Serialized));
            SQLitePCL.raw.sqlite3_enable_shared_cache(1);
            SQLitePCL.raw.sqlite3_initialize();

            MessagingCenter.Subscribe<string>("Setup", "Setup", (obj) =>
            {
                LoadCustomToolBar();
                if (Device.Idiom == TargetIdiom.Phone)
                {
                    RequestedOrientation = ScreenOrientation.Portrait;
                }
            });

            //Added this to get journaling to work found it here: https://stackoverflow.com/questions/4926676/mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { return true; });

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            //RequestAudioFocus();

            var am = (AudioManager)this.GetSystemService(AudioService);
            var componentName = new ComponentName(PackageName, new MediaButtonBroadcastReceiver().ComponentName);
            am.RegisterMediaButtonEventReceiver(componentName);

            Rg.Plugins.Popup.Popup.Init(this, bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);

            AndroidAppLinks.Init(this);
            AndroidBug5497WorkaroundForXamarinAndroid.assistActivity(this);

            //TODO: Replace for journal?
            //DependencyService.Register<SocketService>();
            DependencyService.Register<FileManagement>();
            DependencyService.Register<StripeApiManagement>();
            DependencyService.Register<RivetsService>();
            DependencyService.Register<RecordService>();
            DependencyService.Register<AnalyticsService>();

            CrossCurrentActivity.Current.Init(this, bundle);
            SegmentedControlRenderer.Init();

            SQLite_Droid.Assets = this.Assets;
            var metrics = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetRealMetrics(metrics);
            GlobalResources.Instance.ScreenSize = (int)(metrics.HeightPixels / metrics.Density);
            GlobalResources.Instance.AndroidDensity = metrics.Density;

            LoadApplication(new App());

            MessagingCenter.Subscribe<string>("RecordPermission", "RecordPermission", (sender) => { RequestRecordPermission(); });

            if (Device.Idiom == TargetIdiom.Phone)
            {
                RequestedOrientation = ScreenOrientation.Portrait;
            }


            //Reciever for detecting android phone states, incoming/outgoing calls
            callReceiver = new CallReceiver();
            TelephonyManager telephonyManager = (TelephonyManager)GetSystemService(Context.TelephonyService);
            telephonyManager.Listen(callReceiver, PhoneStateListenerFlags.CallState);
        }

        public override bool DispatchPopulateAccessibilityEvent(AccessibilityEvent e)
        {
            return true;
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            LoadCustomToolBar();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        public override void OnBackPressed()
        {
            if (GlobalResources.Instance.OnRecord)
            {
                MessagingCenter.Send("Back", "Back");
            }
            else
            {
                if (Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed))
                {
                    Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                }
            }
        }

        void LoadCustomToolBar()
        {
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            if (toolbar != null)
            {
                SetSupportActionBar(toolbar);
                var newMenu = LayoutInflater?.Inflate(Resource.Layout.DabToolbar, null);
                //newMenu.ImportantForAutofill = Android.Views.ImportantForAutofill.Yes;
                var menu = (ImageButton)newMenu.FindViewById(Resource.Id.item1);
                menu.ContentDescription = "Menu Button";
                menu.Click += (sender, e) => { MessagingCenter.Send<string>("Menu", "Menu"); };

                var give = (Android.Widget.Button)newMenu.FindViewById(Resource.Id.item2);
                give.SetTextColor(((Xamarin.Forms.Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid());
                give.Click += (sender, e) => { MessagingCenter.Send<string>("Give", "Give"); };

                var record = (ImageButton)newMenu.FindViewById(Resource.Id.item3);
                record.ContentDescription = "Record Button";
                record.Click += (sender, e) => { MessagingCenter.Send("Record", "Record"); };

                var text = (TextView)newMenu.FindViewById(Resource.Id.textView1);
                text.SetTextColor(((Xamarin.Forms.Color)App.Current.Resources["PlayerLabelColor"]).ToAndroid());
                text.Typeface = Typeface.CreateFromAsset(Assets, "FetteEngD.ttf");
                text.TextSize = 30;
                if (GlobalResources.TestMode)
                {
                    var testColor = ((Xamarin.Forms.Color)App.Current.Resources["HighlightColor"]).ToAndroid();
                    newMenu.SetBackgroundColor(testColor);
                }
                toolbar?.AddView(newMenu);
                MessagingCenter.Subscribe<string>("Remove", "Remove", (obj) => { give.Visibility = ViewStates.Invisible; });
                MessagingCenter.Subscribe<string>("Show", "Show", (obj) => { give.Visibility = ViewStates.Visible; });
            }
        }


        //More of what was needed to get journaling to work on Android once again found it here: https://stackoverflow.com/questions/4926676/mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed
        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //Return true if the server certificate is ok
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            bool acceptCertificate = true;
            string msg = "The server could not be validated for the following reason(s):\r\n";

            //The server did not present a certificate
            if ((sslPolicyErrors &
                 SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                msg = msg + "\r\n    -The server did not present a certificate.\r\n";
                acceptCertificate = false;
            }
            else
            {
                //The certificate does not match the server name
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    msg = msg + "\r\n    -The certificate name does not match the authenticated name.\r\n";
                    acceptCertificate = false;
                }

                //There is some other problem with the certificate
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    foreach (X509ChainStatus item in chain.ChainStatus)
                    {
                        if (item.Status != X509ChainStatusFlags.RevocationStatusUnknown &&
                            item.Status != X509ChainStatusFlags.OfflineRevocation)
                            break;

                        if (item.Status != X509ChainStatusFlags.NoError)
                        {
                            msg = msg + "\r\n    -" + item.StatusInformation;
                            acceptCertificate = false;
                        }
                    }
                }
            }

            //If Validation failed, present message box
            if (acceptCertificate == false)
            {
                msg = msg + "\r\nDo you wish to override the security check?";
                //          if (MessageBox.Show(msg, "Security Alert: Server could not be validated",
                //                       MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                acceptCertificate = true;
            }

            return acceptCertificate;
        }

        void RequestRecordPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.RecordAudio) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.RecordAudio }, 1);
            }
        }

        //public bool RequestAudioFocus()
        //{
        //    AudioManager audioManager = (AudioManager)GetSystemService(AudioService);
        //    AudioFocusRequest audioFocusRequest;
        //    if (Build.VERSION.SdkInt > BuildVersionCodes.O)
        //    {
        //        audioFocusRequest = audioManager.RequestAudioFocus(new AudioFocusRequestClass.Builder(AudioFocus.Gain)
        //        .SetAudioAttributes(new AudioAttributes.Builder().SetLegacyStreamType(Android.Media.Stream.Music).Build()).SetOnAudioFocusChangeListener(this)
        //        .Build());
        //    }
        //    else
        //    {
        //        audioFocusRequest = audioManager.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
        //    }

        //    if (audioFocusRequest == AudioFocusRequest.Granted)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        //public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
        //{
        //    DabPlayer player = GlobalResources.playerPodcast;

        //    switch (focusChange)
        //    {
        //        case AudioFocus.Gain:
        //            player.Play();
        //            //Gain when other Music Player app releases the audio service   
        //            break;
        //        case AudioFocus.Loss:
        //            //We have lost focus stop!   
        //            player.Stop();
        //            break;
        //        case AudioFocus.LossTransient:
        //            //We have lost focus for a short time, but likely to resume so pause   
        //            player.Pause();
        //            break;
        //        case AudioFocus.LossTransientCanDuck:
        //            //We have lost focus but should till play at a muted 10% volume   
        //            //player.SetVolume(.1);
        //            break;
        //    }
        //}
    }

    public class AndroidBug5497WorkaroundForXamarinAndroid
    {

        // For more information, see https://code.google.com/p/android/issues/detail?id=5497
        // To use this class, simply invoke assistActivity() on an Activity that already has its content view set.

        // CREDIT TO Joseph Johnson (http://stackoverflow.com/users/341631/joseph-johnson) for publishing the original Android solution on stackoverflow.com

        public static void assistActivity(Activity activity)
        {
            new AndroidBug5497WorkaroundForXamarinAndroid(activity);
        }

        private Android.Views.View mChildOfContent;
        private int usableHeightPrevious;
        private FrameLayout.LayoutParams frameLayoutParams;

        private AndroidBug5497WorkaroundForXamarinAndroid(Activity activity)
        {
            FrameLayout content = (FrameLayout)activity.FindViewById(Android.Resource.Id.Content);
            mChildOfContent = content.GetChildAt(0);
            ViewTreeObserver vto = mChildOfContent.ViewTreeObserver;

            frameLayoutParams = (FrameLayout.LayoutParams)mChildOfContent.LayoutParameters;
        }

        private int computeUsableHeight()
        {
            Rect r = new Rect();
            mChildOfContent.GetWindowVisibleDisplayFrame(r);
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                return (r.Bottom - r.Top);
            }
            return r.Bottom;
        }
    }
}
