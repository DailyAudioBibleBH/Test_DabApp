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

        public DabRecordingPage()
        {
            InitializeComponent();
            AudioPlayer.Instance.DeCouple();
            AudioPlayer.Instance.OnRecord = true;
            Playing = false;
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
            GlobalResources.Instance.OnRecord = true;
            MessagingCenter.Subscribe<string>("Back", "Back", (sender) => {
                OnCancel(this, new EventArgs());
            });
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
                        SeekBar.Value = AudioPlayer.Instance.CurrentTime;
                        SeekBar.IsVisible = true;
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
            var response = await DisplayAlert("Recording will be lost!", "The recording that you worked on so far will be lost do you want to proceed?", "Yes", "No");
            if (response)
            {
                AudioPlayer.Instance.DeCouple();
                viewModel.Recorded = false;
                viewModel.Reviewed = false;
                viewModel.RecordImageUrl = "Record";
                SeekBar.IsVisible = false;
                Playing = false;
                viewModel.RecordingTime = "2:00";
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
                else await DisplayAlert("No Internet Connection", "In order to submit your voice message to the Daily Audio Bible please make sure you have an internet connection", "OK");
            }
        }

        async void OnCancel(object o, EventArgs e)
        {
            var response = await DisplayAlert("Recording will be lost!", "The recording that you worked on so far will be lost do you want to proceed?", "Yes", "No");
            if(response)
            {
                AudioPlayer.Instance.DeCouple();
                //if (AudioPlayer.Instance.CurrentEpisodeId != 0)
                //{
                //    dbEpisodes episode = new dbEpisodes();
                //    AudioPlayer.Instance.SetAudioFile(episode);
                //}
                await Navigation.PopModalAsync();
            }
        }

        protected override void OnDisappearing()
        {
            AudioPlayer.Instance.MinTimeToSkip = 5;
            AudioPlayer.Instance.DeCouple();
            MessagingCenter.Unsubscribe<string>("Back", "Back");
            GlobalResources.Instance.OnRecord = false;
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
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught while sending audio via email: {ex.Message}");
                await DisplayAlert("Audio failed to send", "An error has been encountered and the audio has not been sent we're sorry for the inconvenience", "OK");
                return false;
            }
        }
    }
}