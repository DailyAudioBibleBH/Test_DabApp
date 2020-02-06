using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabViewHelpers
{
    public class IndexedEpisodes
    {
        public IndexedEpisodes(EpisodeViewModel currentEp, EpisodeViewModel previousEp, EpisodeViewModel nextEp, int currentIndex, int previousEpIndex, int nextEpIndex, int count)
        {
            this.currentEp = currentEp;
            this.previousEp = previousEp;
            this.nextEp = nextEp;
            this.currentIndex = currentIndex;
            this.previousEpIndex = previousEpIndex;
            this.nextEpIndex = nextEpIndex;
            this.count = count;
        }

        public EpisodeViewModel currentEp { get; set; }
        public EpisodeViewModel previousEp { get; set; }
        public EpisodeViewModel nextEp { get; set; }
        public int currentIndex { get; set; }
        public int previousEpIndex { get; set; }
        public int nextEpIndex { get; set; }
        public int count { get; set; }
    }
}
