﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Environment = System.Environment;

namespace DABApp.Droid
{
    public class RecordService : IRecord
    {
        public bool IsRecording { get; set; }

        public event EventHandler<RecordingHandler> AudioWaves;

        private MediaRecorder recorder;
        private string fileName;

        public string StartRecording()
        {
            var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName = Path.Combine(doc, $"DABRecording.mp3");
            double MaxAmp = 1;

            recorder = new MediaRecorder();
            recorder.SetAudioSource(AudioSource.Mic);
            recorder.SetOutputFormat(OutputFormat.Mpeg4);
            recorder.SetAudioEncoder(AudioEncoder.Aac);
            recorder.SetOutputFile(fileName);
            recorder.Prepare();
            recorder.Start();
            IsRecording = true;
            new Thread(() => {
                Thread.CurrentThread.IsBackground = true;
                Device.StartTimer(TimeSpan.FromMilliseconds(10), () => {
                    if (!IsRecording) return false;
                    MaxAmp = recorder.MaxAmplitude/32767.00*100.00;
                    Console.WriteLine($"{MaxAmp}");
                    AudioWaves?.Invoke(this, new RecordingHandler(MaxAmp, 0));
                    return true;
                });
            }).Start();
            return fileName;
        }

        public string StopRecording()
        {
            recorder.Stop();
            IsRecording = false;
            return fileName;
        }
    }
}