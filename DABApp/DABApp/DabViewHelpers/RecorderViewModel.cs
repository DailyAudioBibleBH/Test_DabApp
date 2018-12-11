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
        private bool isRecording;
        private double recentAveragePower = 1;
        private double middleAveragePower = 1;
        private double lastAveragePower = 1;
        private string recordingTime = "2:00";

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
            }
        }

        public string RecordingTime { get { return recordingTime; }
            set { recordingTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecordingTime"));
            } }

        public void StartRecording()
        {
            DependencyService.Get<IRecord>().StartRecording();
            IsRecording = true;
            TimeSpan maxTime = TimeSpan.FromSeconds(15);
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
                    }
                    RecordingTime = "2:00";
                    return false;
                }
            });
        }

        public void StopRecording()
        {
            AudioFile = DependencyService.Get<IRecord>().StopRecording();
            IsRecording = false;
        }
    }
}
