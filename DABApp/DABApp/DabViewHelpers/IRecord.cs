using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DABApp
{
    public interface IRecord
    {
        string StartRecording();
        string StopRecording();
        bool IsRecording { get; set; }
        event EventHandler<RecordingHandler> AudioWaves;
    }
}
