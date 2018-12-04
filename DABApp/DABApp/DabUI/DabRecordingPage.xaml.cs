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
        public AudioRecorderService recorder;

        public DabRecordingPage()
        {
            InitializeComponent();
            recorder = new AudioRecorderService() {
                StopRecordingAfterTimeout = true,
                TotalAudioTimeout = TimeSpan.FromMinutes(2),
                StopRecordingOnSilence = false
            };
            recorder.AudioInputReceived += audioInputReceived;
        }

        async void OnRecord(object o, EventArgs e)
        {
            await RecordAudio();
        }

        async Task RecordAudio()
        {
            try
            {
                if (!recorder.IsRecording)
                {
                    await recorder.StartRecording();
                }
                else
                {
                    await recorder.StopRecording();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught while recording: {ex.Message}");
            }
        }

        async void audioInputReceived(object sender, string audioFile)
        {
            if (audioFile != null)
            {
                
            }
        }

        void OnPlay(object o, EventArgs e)
        {
            if (AudioPlayer.Instance.IsInitialized)
            {
                if (AudioPlayer.Instance.IsPlaying)
                {
                    AudioPlayer.Instance.Pause();
                }
                else AudioPlayer.Instance.Play();
            }
            else
            {
                var audio = recorder.GetAudioFilePath();
                if (audio != null)
                {
                    AudioPlayer.Instance.SetAudioFile(audio);
                    AudioPlayer.Instance.Play();
                }
            }
        }

        async void OnSubmit(object o, EventArgs e)
        {
            var audio = recorder.GetAudioFilePath();
            if (audio != null)
            {
                if (CrossConnectivity.Current.IsConnected)
                {
                    SendAudio(audio);
                }
                else DisplayAlert("No Internet Connection", "In order to submit your voice message to the Daily Audio Bible please make sure you have an internet connection", "OK");
            }
        }

        void SendAudio(string fileName)
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
                smtp.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught while sending audio via email: {ex.Message}");
                DisplayAlert("Audio failed to send", "An error has been encountered and the audio has not been sent we're sorry for the inconvenience", "OK");
            }
        }
    }
}