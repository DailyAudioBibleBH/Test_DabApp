using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DABApp
{
    public class DabEventArgs: EventArgs
    {
        public double ProgressPercentage { get; set; }
        public int EpisodeId { get; set; }
        public bool Cancelled { get; set; }

        public DabEventArgs(int id, double percent, bool cancelled = false)
        {
            ProgressPercentage = percent;
            EpisodeId = id;
            Cancelled = cancelled;
        }
    }

    public class RecordingHandler : EventArgs
    {
        public double AveragePower { get; set; }
        public double PeakPower { get; set; }

        public RecordingHandler(double averagePower, double peakPower)
        {
            AveragePower = averagePower;
            PeakPower = peakPower;
        }
    }
}
