﻿using System;
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

        public DabEventArgs(int id, double percent)
        {
            ProgressPercentage = percent;
            EpisodeId = id;
        }
    }
}