using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.Helpers
{
    public class dabUserBadgeProgress
    {
        dbBadges badge;
        dbBadgeProgress progress;
        public dabUserBadgeProgress(dbBadges badge, dbBadgeProgress progress)
        {
            this.badge = badge;
            this.progress = progress;
        }
    }
}
