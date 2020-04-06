using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.Helpers
{
    public class dabUserBadgeProgress
    {
        public dbBadges Badge { get; set; }
        public dbUserBadgeProgress Progress { get; set; }

        public dabUserBadgeProgress(dbBadges badge, dbUserBadgeProgress progress)
        {
            this.Badge = badge;
            this.Progress = progress;
        }
    }
}
