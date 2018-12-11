using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using UIKit;
using Xamarin.Forms;

namespace DABApp.iOS
{
    public class RecordService : IRecord
    {
        public event EventHandler<RecordingHandler> AudioWaves;

        AVAudioRecorder recorder;

        public bool IsRecording { get; set; }

        public string StartRecording()
        {
            var audioSession = AVAudioSession.SharedInstance();
            var err = audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord);
            err = audioSession.SetActive(true);
            var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var fileName = Path.Combine(doc, $"DABRecording.wav");
            var settings = new AudioSettings();
            var error = new NSError();
            recorder = AVAudioRecorder.Create(new NSUrl(fileName), settings, out error);
            recorder.MeteringEnabled = true;
            double averagePower;
            double peakPower;
            recorder.Record();
            IsRecording = true;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true) { 
                    if (!recorder.Recording) return;
                    recorder.UpdateMeters();
                    averagePower = (recorder.AveragePower(0) + 50);
                    peakPower = recorder.PeakPower(0);
                    AudioWaves?.Invoke(this, new RecordingHandler(averagePower, peakPower));
                    //Console.WriteLine($"{DateTime.Now} {averagePower} : {peakPower}");
                    Thread.Sleep(100);
                }
            }).Start();
            return fileName.ToString();
        }

        public string StopRecording()
        {
            recorder.Stop();
            IsRecording = false;
            return recorder.Url.ToString();
        }
    }
}