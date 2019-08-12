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
using DABApp.DabAudio;

namespace DABApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DabRecordingPage : DabBaseContentPage
    {
        AudioRecorderService recorder;
        RecorderViewModel viewModel;
        DabPlayer player = GlobalResources.playerRecorder;
        bool granted = true;
        double _width;
        double _height;

        public DabRecordingPage()
        {
            InitializeComponent();

            // TODO: Set up bindings removed from XAML:
            // Timer Text < RecordingTime           
            // Record Source < RecordImageUrl
            // lblGuide Text < GuideText
            // Delete IsVisible < Recorded
            // SeekBar < Value="{Binding Source={x:Reference player}, Path=CurrentPosition}" 
            // SeekBar < Maximum="{Binding Source={x:Reference player}, Path=Duration}"
            // AudioVisualizer <  IsVisible="{Binding IsRecording}"
            // Submit < IsVisible="{Binding Recorded}"

            //Set up bindings
            viewModel = new RecorderViewModel();

            //Record Button
            Record.BindingContext = viewModel;
            Record.SetBinding(Image.SourceProperty, "RecordImageUrl");

            //Timer
            Timer.BindingContext = viewModel;
            Timer.SetBinding(Label.TextProperty, "RecordingTime");

            //Guide
            lblGuide.BindingContext = viewModel;
            lblGuide.SetBinding(Label.TextProperty, "GuideText");

            //Delete
            Delete.BindingContext= viewModel;
            Delete.SetBinding(Button.IsVisibleProperty, "Recorded");

            //Audio Visualizer
            AudioVisualizer.BindingContext = viewModel;
            AudioVisualizer.SetBinding(StackLayout.IsVisibleProperty, "IsRecording");

            //Submit Button
            Submit.BindingContext = viewModel;
            Submit.SetBinding(Button.IsVisibleProperty, "Recorded");

            //PLAYER BINDINGS
            SeekBar.BindingContext = player;
            SeekBar.SetBinding(Slider.ValueProperty, "CurrentPosition");
            SeekBar.SetBinding(Slider.MaximumProperty, "Duration");






            if (Device.RuntimePlatform == Device.Android)
            {
                MessagingCenter.Send<string>("RecordPermission", "RecordPermission");
            }
            else granted = DependencyService.Get<IRecord>().RequestMicrophone();
            //AudioPlayer.Instance.DeCouple();
            Destination.ItemsSource = GlobalResources.Instance.PodcastEmails.Select(x => x.Podcast).ToList();
            Destination.SelectedIndex = Device.RuntimePlatform == Device.iOS ? 0 : -1;
            GlobalResources.Instance.OnRecord = true;
            base.ControlTemplate = (ControlTemplate)Application.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];
            recorder = new AudioRecorderService()
            {
                StopRecordingAfterTimeout = true,
                TotalAudioTimeout = TimeSpan.FromMinutes(2),
                StopRecordingOnSilence = false
            };
            recorder.AudioInputReceived += audioInputReceived;

            //Add vizualizer elements
            for (int x = ((viewModel.AudioHistoryCount * -1)+1); x <= viewModel.AudioHistoryCount-1; x++)
            {
                BoxView box = new BoxView();
                box.BindingContext = viewModel;
                box.Color = (Color)App.Current.Resources["HighlightColor"];
                box.Opacity = (double)1 - ((double)Math.Abs(x) / (double)viewModel.AudioHistoryCount); //Fade out towards the edges
                box.SetBinding(BoxView.HeightRequestProperty, new Binding($"AudioHistory[{Math.Abs(x)}]")); //Older data toward the edges
                box.SetBinding(BoxView.IsVisibleProperty, new Binding("IsRecording"));
                box.WidthRequest = 10;
                box.HorizontalOptions = LayoutOptions.CenterAndExpand;
                box.VerticalOptions = LayoutOptions.CenterAndExpand;
                AudioVisualizer.Children.Add(box);
            }
            
            lblGuide.BindingContext = viewModel;
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
            if (player.IsPlaying)
            {
                player.Pause();
            }
            MessagingCenter.Subscribe<string>("Back", "Back", (sender) =>
            {
                OnCancel(this, new EventArgs());
            });
            var tapper = new TapGestureRecognizer();
            tapper.Tapped += OnRecord;
            Record.GestureRecognizers.Add(tapper);
            viewModel.EndOfTimeLimit += EndOfTime;
            Destination.Submitted += Destination_Submitted;
        }

        async void OnRecord(object o, EventArgs e)
        {
            granted = DependencyService.Get<IRecord>().RequestMicrophone();
            if (!granted)
            {
                var response = await DisplayAlert("Microphone permission required", "DAB needs access to your microphone in order to record would you like to go to settings and enable microphone access", "Go to Settings", "No");
                if (response)
                {
                    DependencyService.Get<IRecord>().GoToSettings();
                }
                granted = DependencyService.Get<IRecord>().RequestMicrophone();
            }
            else
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
                        Record.BindingContext = player;
                        Record.SetBinding(Image.SourceProperty, new Binding("PlayPauseButtonImageBig"));
                        Timer.BindingContext = player;
                        var converter = new StringConverter();
                        converter.onRecord = true;
                        Timer.SetBinding(Label.TextProperty, new Binding("Duration", BindingMode.Default, converter, null, null, player));
                        SeekBar.Value = player.CurrentPosition;
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
            DependencyService.Get<IAnalyticsService>().LogEvent("recording_played");
            viewModel.Reviewed = true;
            if (player.IsReady)
            {
                if (player.IsPlaying)
                {
                    player.Pause();
                } else
                {
                    player.Play();
                }
            }
            else
            {
                var audio = viewModel.AudioFile;
                if (audio != null)
                {
                    player.Load(audio);
                    player.Play();
                }
                SeekBar.Value = player.CurrentPosition;
                SeekBar.IsVisible = true;
            }
        }

        async void OnDelete(object o, EventArgs e)
        {
            var response = await DisplayAlert("Recording will be lost", "Are you sure you want to cancel your audio recording? Your current recording will be lost.", "Yes", "No");
            if (response)
            {
                //AudioPlayer.Instance.DeCouple();
                viewModel.Recorded = false;
                viewModel.Reviewed = false;
                SeekBar.IsVisible = false;
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
            if (!viewModel.Reviewed)
            {
                viewModel.Reviewed = await DisplayAlert("Submitting without review", "You're about to submit your recording without reviewing it. Would you like to review your recording before you submit it?", "Submit Now", "Review");
            }
            if (viewModel.Reviewed)
            {
                Destination.IsEnabled = true;
                //Destination.IsVisible = true;
                Destination.Focus();
            }
            else
            {
                OnPlay();
            }
        }

        private async void Destination_Submitted(object sender, EventArgs e)
        {
            Destination.Unfocus();
            Destination.IsVisible = false;
            Destination.IsEnabled = false;
            var audio = viewModel.AudioFile;
            if (audio != null)
            {
                if (CrossConnectivity.Current.IsConnected)
                {
                    ActivityIndicator activity = ControlTemplateAccess.FindTemplateElementByName<ActivityIndicator>(this, "activity");
                    StackLayout labelHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "labelHolder");
                    StackLayout activityHolder = ControlTemplateAccess.FindTemplateElementByName<StackLayout>(this, "activityHolder");
                    Label explanation = ControlTemplateAccess.FindTemplateElementByName<Label>(this, "Explanation");
                    activity.IsVisible = true;
                    activityHolder.IsVisible = true;
                    labelHolder.IsVisible = true;
                    activity.VerticalOptions = LayoutOptions.EndAndExpand;
                    explanation.IsVisible = true;
                    var result = await SendAudio(audio);
                    activity.IsVisible = false;
                    activityHolder.IsVisible = false;
                    labelHolder.IsVisible = false;
                    explanation.IsVisible = false;
                    if (result) await Navigation.PopModalAsync();
                }
                else await DisplayAlert("No Internet Connection", "Your audio recording could not be submitted at this time. Please check your network connection and try again.", "OK");
            }
        }

        async void OnCancel(object o, EventArgs e)
        {
            var response = await DisplayAlert("Recording will be lost", "Are you sure you want to cancel your audio recording? Your current recording will be lost.", "Yes", "No");
            if (response)
            {
                await Navigation.PopModalAsync();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            //AudioPlayer.Instance.MinTimeToSkip = 5;
            //AudioPlayer.Instance.DeCouple();
            MessagingCenter.Unsubscribe<string>("Back", "Back");
            //if (AudioPlayer.Instance.CurrentEpisodeId != 0)
            //{
            //    dbEpisodes episode = new dbEpisodes();
            //    AudioPlayer.RecordingInstance.SetAudioFile(episode);
            //}
            //AudioPlayer.RecordingInstance.OnRecord = false;
            GlobalResources.Instance.OnRecord = false;
            //SeekBar.RemoveBinding(Slider.ValueProperty);
            if (viewModel.IsRecording)
            {
                viewModel.StopRecording();
            }
            if (player.IsPlaying)
            {
                player.Stop();
            }
            base.OnDisappearing();
        }

        async Task<bool> SendAudio(string fileName)
        {
            try
            {

                //Send the audio file as an email attachment

                //Get the email address of the podcast
                PodcastEmail podcastEmail = GlobalResources.Instance.PodcastEmails[Destination.SelectedIndex];

                //Start a new mail message with proper destination emails
                var mailMessage = new MailMessage("noreply@c2itconsulting.net", podcastEmail.Email);
                mailMessage.Bcc.Add("alerts_dab@c2itconsulting.net");

                //Build the message content
                mailMessage.Subject = $"{podcastEmail.Podcast} Audio Recording: {GlobalResources.GetUserName()} at {DateTime.Now.ToString()}";
                mailMessage.IsBodyHtml = true;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"<h1>New Audio Recording Submission</h1>");
                sb.AppendLine($"<p>User: {GlobalResources.GetUserName()}</p>");
                sb.AppendLine($"<p>Timestamp: {DateTime.Now.ToString()}</p>");
                sb.AppendLine($"<p>Platform: {Device.RuntimePlatform}</p>");
                sb.AppendLine($"<p>Idiom: {Device.Idiom.ToString()}</p>");
                mailMessage.Body = sb.ToString();

                //Attach the file
                var att = new Attachment(fileName, "audio/wav");
                mailMessage.Attachments.Add(att);

                //Set up the SMTP client using Mandril API credentials
                var smtp = new SmtpClient();
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Host = "smtp.mandrillapp.com";
                smtp.Credentials = new NetworkCredential("chetcromer@c2itconsulting.net", "-M0yjVB_9EqZEzuKUDjw3A");
                smtp.EnableSsl = true;

                //Send the email
                await smtp.SendMailAsync(mailMessage);

                //Let the user know it was sent.
                await DisplayAlert("Success!", $"Your audio recording has been successfully submitted for the {podcastEmail.Podcast}.", "OK");

                //Sending Event to Firebase Analytics indicating user has successfully submitted a recording.
                DependencyService.Get<IAnalyticsService>().LogEvent("recording_submitted");

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
                r4.Height = new GridLength(1, GridUnitType.Star);
                r3.Height = new GridLength(1, GridUnitType.Star);
                r7.Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                if (Device.Idiom == TargetIdiom.Tablet)
                {
                    r4.Height = new GridLength(.5, GridUnitType.Star);
                    r3.Height = new GridLength(.5, GridUnitType.Star);
                    r7.Height = new GridLength(.5, GridUnitType.Star);
                }
            }
        }

        void EndOfTime(object o, EventArgs e)
        {
            DependencyService.Get<IAnalyticsService>().LogEvent("recording_limitreached");
            Record.BindingContext = player;
            //TODO: Create this property for the player
            Record.SetBinding(Image.SourceProperty, new Binding("PlayPauseButtonImageBig"));
            Timer.BindingContext = player;
            var converter = new StringConverter();
            converter.onRecord = true;
            //Timer.SetBinding(Label.TextProperty, new Binding("Duration", BindingMode.Default, converter, null, null, player));
            Timer.Text = "2:00";
            SeekBar.Value = player.CurrentPosition;
            SeekBar.IsVisible = true;
            c0.Width = new GridLength(2, GridUnitType.Star);
            c1.Width = new GridLength(2, GridUnitType.Star);
            Cancel.Margin = new Thickness(5, 10, 10, 10);
            DisplayAlert("Time Limit Reached", "Each recording is limited to 2:00 minutes. Please review your recording that has been cut off or re-record and limit yourself to 2:00" +
                " minutes.", "OK");
        }
    }
}