using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using DABApp.DabAudio;
using Xamarin.Forms;

namespace DABApp
{
    public class RecorderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler EndOfTimeLimit;
        private bool isRecording;
        private double recentAveragePower = 1;
        private string recordingTime = "2:00";
        private bool recorded;
        private bool reviewed;
        private ObservableCollection<double> _audioHistory = new ObservableCollection<double>() { 10, 20, 30, 40, 50,60,70,80,90,100 }; //Visualizer Initial Values
        private DabPlayer player = GlobalResources.playerRecorder;

        public RecorderViewModel()
        {
            DependencyService.Get<IRecord>().AudioWaves += UpdateWave;
        }

        public int AudioHistoryCount
        {
            get
            {
                return AudioHistory.Count;
            }
        }

        private void UpdateWave(object sender, RecordingHandler e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                //Shift all elements of the array
                for (int x = _audioHistory.Count - 1; x > 0; x--)
                {
                    _audioHistory[x] = _audioHistory[x - 1];
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"AudioHistory[{x}]"));
                }
                //Store the most recent poswer
                _audioHistory[0] = e.AveragePower;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"AudioHistory[0]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"AudioHistory"));
                RecentAveragePower = e.AveragePower;
            });
        }

        public string AudioFile { get; private set; }


        public ObservableCollection<double> AudioHistory
        {
            get
            {
                return _audioHistory;
            }
            set
            {
                _audioHistory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AudioHistory"));
            }
        }

        public double RecentAveragePower
        {
            get
            {
                return recentAveragePower;
            }
            set {
                recentAveragePower = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecentAveragePower"));
            }
        }

        public bool IsRecording
        {
            get
            {
                return isRecording;
            }
            set
            {
                isRecording = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRecording"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecordImageUrl"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GuideText"));
            }
        }

        public bool Recorded
        {
            get
            {
                return recorded;
            }
            set
            {
                recorded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Recorded"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GuideText"));
            }
        }

        public bool Reviewed
        {
            get
            {
                return reviewed;
            }
            set
            {
                reviewed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Reviewed"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GuideText"));
            }
        }

        public string RecordingTime
        {
            get
            {
                return recordingTime;
            }
            set
            {
                recordingTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecordingTime"));
            }
        }

        public string RecordImageUrl
        {
            get
            {
                if (isRecording)
                {
                    return "stop.png";
                }
                else return "microphone.png";
            }
            set
            {
                throw new Exception("This can't be set directly");
            }
        }

        public string GuideText
            //Text beneath button to guide the user to what they should do now.
        {
            get
            {
                if (!isRecording && !reviewed && !recorded) //New recording - Not recording, not reviewed, and not recorded 
                {
                    return "Tap the microphone to begin your recording.";
                } else if (isRecording) //Currently being recorded
                {
                    return "Tap the stop button to complete your recording.";
                } else if (recorded && !reviewed) //Recorded but not reviewed
                {
                    return "Tap the play button to review your recording.";
                } else //Recorded and Reviewed
                {
                    return "Tap the submit button to submit your recording.";
                }
            }
            set
            {
                throw new Exception("This can't be set directly");
            }
        }


        public void StartRecording()
        {
            DependencyService.Get<IAnalyticsService>().LogEvent("recording_started");
            DependencyService.Get<IRecord>().StartRecording();
            IsRecording = true;
            TimeSpan maxTime = TimeSpan.FromSeconds(119);
            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (IsRecording && maxTime > TimeSpan.FromSeconds(0))
                {
                    maxTime = maxTime - TimeSpan.FromSeconds(1);
                    RecordingTime = maxTime.ToString(@"m\:ss");
                    return true;
                }
                else
                {
                    if (IsRecording)
                    {
                        StopRecording();
                        EndOfTimeLimit?.Invoke(this, new EventArgs());
                    }
                    //RecordingTime = "2:00";
                    //Recorded = true;
                    return false;
                }
            });
        }

        public void StopRecording()
        {
            //Sending Event to Firebase Analytics that indicates user recorded a file.
            DependencyService.Get<IAnalyticsService>().LogEvent("recording_recorded");

            AudioFile = DependencyService.Get<IRecord>().StopRecording();
            Recorded = true;
            IsRecording = false;
            player.Load(AudioFile);
        }
    }
}
