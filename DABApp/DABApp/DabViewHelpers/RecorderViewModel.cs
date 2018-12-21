using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace DABApp
{
    public class RecorderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler EndOfTimeLimit;
        private bool isRecording;
        private double recentAveragePower = 1;
        private double middleAveragePower = 1;
        private double lastAveragePower = 1;
        private string recordingTime = "2:00";
        private bool recorded;
        private bool reviewed;

        public RecorderViewModel()
        {
            DependencyService.Get<IRecord>().AudioWaves += UpdateWave;
        }

        private void UpdateWave(object sender, RecordingHandler e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                LastAveragePower = MiddleAveragePower;
                MiddleAveragePower = RecentAveragePower;
                RecentAveragePower = e.AveragePower;
               
            });
        }

        public string AudioFile { get; private set; }
        

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

        public double MiddleAveragePower {
            get {
                return middleAveragePower;
            }
            set {
                middleAveragePower = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MiddleAveragePower"));
            }
        }

        public double LastAveragePower {
            get
            {
                return lastAveragePower;
            }
            set
            {
                lastAveragePower = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastAveragePower"));
            }
        }

        public bool IsRecording {
            get {
                return isRecording;
            }
            set
            {
                isRecording = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRecording"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecordImageUrl"));
            }
        }

        public bool Recorded
        {
            get
            {
                return recorded;
            }
            set {
                recorded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Recorded"));
            }
        }

        public bool Reviewed {
            get {
                return reviewed;
            }
            set {
                reviewed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Reviewed"));
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


        public void StartRecording()
        {
            DependencyService.Get<IRecord>().StartRecording();
            IsRecording = true;
            TimeSpan maxTime = TimeSpan.FromSeconds(119);
            Device.StartTimer(TimeSpan.FromSeconds(1), () => {
                if (IsRecording && maxTime > TimeSpan.FromSeconds(0))
                {
                    maxTime = maxTime - TimeSpan.FromSeconds(1);
                    RecordingTime = maxTime.ToString(@"m\:ss");
                    return true;
                }
                else {
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
            AudioFile = DependencyService.Get<IRecord>().StopRecording();
            Recorded = true;
            IsRecording = false;
            AudioPlayer.Instance.SetAudioFile(AudioFile);
        }
    }
}
