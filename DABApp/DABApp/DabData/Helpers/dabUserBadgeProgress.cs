using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.Helpers
{
    public class dabUserBadgeProgress
    {
        public dbBadges Badge { get; set; }
        public dbBadgeProgress Progress { get; set; }

        public dabUserBadgeProgress(dbBadges badge, dbBadgeProgress progress)
        {
            this.Badge = badge;
            this.Progress = progress;
        }
    }
}
