using Plugin.AudioRecorder;
using Plugin.Connectivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net;
using System.IO;

namespace DABApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DabRecordingPage : DabBaseContentPage
    {
        AudioRecorderService recorder;
        RecorderViewModel viewModel;
        bool Playing;
        double _width;
        double _height;

        public DabRecordingPage()
        {
            InitializeComponent();
            banner.Aspect = Device.RuntimePlatform == Device.Android ? Aspect.Fill : Aspect.AspectFill;
            AudioPlayer.Instance.DeCouple();
            AudioPlayer.Instance.OnRecord = true;
            Playing = false;
            //if (Device.Idiom == TargetIdiom.Tablet)
            //{
            //    Main.Padding = new Thickness(10, 10, 10, 0);
            //}
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];
            recorder = new AudioRecorderService() {
                StopRecordingAfterTimeout = true,
                TotalAudioTimeout = TimeSpan.FromMinutes(2),
                StopRecordingOnSilence = false
            };
            Playing = false;
            recorder.AudioInputReceived += audioInputReceived;
            viewModel = new RecorderViewModel();
            AudioVisualizer.BindingContext = viewModel;
            Timer.BindingContext = viewModel;
            Submit.BindingContext = viewModel;
            Delete.BindingContext = viewModel;
            Record.BindingContext = viewModel;
            SeekBar.IsVisible = false;
            banner.Source = new UriImageSource()
            {
                Uri = new Uri(ContentConfig.Instance.views.First().banner.urlPhone),
                CacheValidity = GlobalResources.ImageCacheValidity
            };
            AudioPlayer.Instance.MinTimeToSkip = 1;
            if (AudioPlayer.Instance.IsPlaying) AudioPlayer.Instance.DeCouple();
            MessagingCenter.Subscribe<string>("Back", "Back", (sender) => {
                OnCancel(this, new EventArgs());
            });
            var tapper = new TapGestureRecognizer();
            tapper.Tapped += OnRecord;
            Record.GestureRecognizers.Add(tapper);
            viewModel.EndOfTimeLimit += EndOfTime;
        }

        async void OnRecord(object o, EventArgs e)
        {
            if (viewModel.Recorded)
            {
                OnPlay();
            }
            else
            {
                await RecordAudio();
            }

        }

        async Task RecordAudio()
        {
            if (CrossConnectivity.Current.IsConnected)
            {
                try
                {
                    if (!viewModel.IsRecording)
                    {
                        //await recorder.StartRecording();
                        viewModel.StartRecording();
                    }
                    else
                    {
                        //await recorder.StopRecording();
                        viewModel.StopRecording();
                        Record.BindingContext = AudioPlayer.Instance;
                        Record.SetBinding(Image.SourceProperty, new Binding("PlayPauseButtonImageBig"));
                        Timer.BindingContext = AudioPlayer.Instance;
                        Timer.SetBinding(Label.TextProperty, new Binding("TotalTime", BindingMode.Default, new StringConverter(true), null, null, AudioPlayer.Instance));
                        SeekBar.Value = AudioPlayer.Instance.CurrentTime;
                        SeekBar.IsVisible = true;
                        c0.Width = new GridLength(2, GridUnitType.Star);
                        c1.Width = new GridLength(2, GridUnitType.Star);
                        Cancel.Margin = new Thickness(5, 10, 10, 10);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception caught while recording: {ex.Message}");
                }
            }
            else await DisplayAlert("No Internet Connection", "In order to submit your voice message to the Daily Audio Bible please make sure you have an internet connection", "OK");
        }

        void audioInputReceived(object sender, string audioFile)
        {
            Debug.WriteLine($"{recorder.AudioStreamDetails.BitsPerSample}");
        }

        void OnPlay()
        {
            viewModel.Reviewed = true;
            if (AudioPlayer.Instance.IsInitialized)
            {
                if (AudioPlayer.Instance.IsPlaying)
                {
                    AudioPlayer.Instance.Pause();
                    Playing = true;
                }
                else
                {
                    AudioPlayer.Instance.Play();
                    Playing = false;
                }
            }
            else
            {
                var audio = viewModel.AudioFile;
                if (audio != null)
                {
                    AudioPlayer.Instance.SetAudioFile(audio);
                    AudioPlayer.Instance.Play();
                }
                SeekBar.Value = AudioPlayer.Instance.CurrentTime;
                SeekBar.IsVisible = true;
                Playing = true;
            }
        }

        void OnTouch(object o, EventArgs e)
        {
            AudioPlayer.Instance.IsTouched = true;
        }

        async void OnDelete(object o, EventArgs e)
        {
            var response = await DisplayAlert("Recording will be lost", "Are you sure you want to cancel your audio recording? Your current recording will be lost.", "Yes", "No");
            if (response)
            {
                AudioPlayer.Instance.DeCouple();
                viewModel.Recorded = false;
                viewModel.Reviewed = false;
                SeekBar.IsVisible = false;
                Playing = false;
                viewModel.RecordingTime = "2:00";
                Record.BindingContext = viewModel;
                Record.SetBinding(Image.SourceProperty, new Binding("RecordImageUrl"));
                Timer.BindingContext = viewModel;
                Timer.SetBinding(Label.TextProperty, new Binding("RecordingTime"));
                c0.Width = new GridLength(0);
                c1.Width = new GridLength(1, GridUnitType.Star);
                Cancel.Margin = new Thickness(10);
            }
        }

        async void OnSubmit(object o, EventArgs e)
        {
            var audio = viewModel.AudioFile;
            if (audio != null)
            {
                if (CrossConnectivity.Current.IsConnected)
                {
                    var result = await SendAudio(audio);
                    if (result) await Navigation.PopModalAsync();
                }
                else await DisplayAlert("No Internet Connection", "Your audio recording could not be submitted at this time. Please check your network connection and try again.", "OK");
            }
        }

        async void OnCancel(object o, EventArgs e)
        {
            var response = await DisplayAlert("Recording will be lost", "Are you sure you want to cancel your audio recording? Your current recording will be lost.", "Yes", "No");
            if(response)
            {
                await Navigation.PopModalAsync();
            }
        }

        protected override void OnDisappearing()
        {
            AudioPlayer.Instance.MinTimeToSkip = 5;
            AudioPlayer.Instance.DeCouple();
            MessagingCenter.Unsubscribe<string>("Back", "Back");
            if(AudioPlayer.Instance.CurrentEpisodeId != 0)
            {
                dbEpisodes episode = new dbEpisodes();
                AudioPlayer.Instance.SetAudioFile(episode);
            }
            AudioPlayer.Instance.OnRecord = false;
            SeekBar.RemoveBinding(Slider.ValueProperty);
            base.OnDisappearing();
        }

        async Task<bool> SendAudio(string fileName)
        {
            try
            {
                var mailMessage = new MailMessage("chetcromer@c2itconsulting.net", "vicfinney@c2itconsulting.net");
                var smtp = new SmtpClient();
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Host = "smtp.mandrillapp.com";
                var attatchment = new Attachment(fileName, "audio/wav");
                mailMessage.Body = "";
                mailMessage.Attachments.Add(attatchment);
                smtp.Credentials = new NetworkCredential("chetcromer@c2itconsulting.net", "-M0yjVB_9EqZEzuKUDjw3A");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(mailMessage);
                await DisplayAlert("Success!", "Your audio recording has been successfully submitted for the Daily Audio Bible.", "OK");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught while sending audio via email: {ex.Message}");
                await DisplayAlert("Submission Failed", "Your audio recording could not be submitted at this time. Please check your network connection and try again.", "OK");
                return false;
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            double oldwidth = _width;
            base.OnSizeAllocated(width, height);
            if (Equals(_width, width) && Equals(_height, height)) return;
            _width = width;
            _height = height;
            if (Equals(oldwidth, -1)) return;
            if (width > height)
            {
                r6.Height = new GridLength(1, GridUnitType.Star);
                r4.Height = new GridLength(1, GridUnitType.Star);
                r3.Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                if (Device.Idiom == TargetIdiom.Tablet)
                {
                    r6.Height = new GridLength(.5, GridUnitType.Star);
                    r4.Height = new GridLength(.5, GridUnitType.Star);
                    r3.Height = new GridLength(.5, GridUnitType.Star);
                }
            }
        }

        void EndOfTime(object o, EventArgs e)
        {
            Record.BindingContext = AudioPlayer.Instance;
            Record.SetBinding(Image.SourceProperty, new Binding("PlayPauseButtonImageBig"));
            Timer.BindingContext = AudioPlayer.Instance;
            Timer.SetBinding(Label.TextProperty, new Binding("TotalTime", BindingMode.Default, new StringConverter(true), null, null, AudioPlayer.Instance));
            SeekBar.Value = AudioPlayer.Instance.CurrentTime;
            SeekBar.IsVisible = true;
            c0.Width = new GridLength(2, GridUnitType.Star);
            c1.Width = new GridLength(2, GridUnitType.Star);
            Cancel.Margin = new Thickness(5, 10, 10, 10);
            DisplayAlert("Time Limit Reached", "Each recording is limited to 2:00 minutes. Please review your recording that has been cut off or re-record and limit yourself to 2:00" +
                " minutes.", "OK");
        }
    }
}