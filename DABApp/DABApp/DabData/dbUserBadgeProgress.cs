using DABApp.DabSockets;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp
{
    public class dbUserBadgeProgress
    {
        private DabGraphQlProgress progress;

        public dbUserBadgeProgress(DabGraphQlProgress progress, string userName)
        {
            this.userName = userName;
            this.id = progress.id;
            this.data = progress.data;
            this.badgeId = progress.badgeId;
            this.percent = progress.percent;
            this.year = progress.year;
            this.seen = progress.seen;
            this.createdAt = progress.createdAt;
            this.updatedAt = progress.updatedAt;
            this.progressColor = "Blue";
            if (progress.percent == 100)
            {
                this.tint = "Transparent";
            }
            else
            {
                this.tint = "Gray";
            }
        }

        public dbUserBadgeProgress()
        {

        }

        public dbUserBadgeProgress(int badgeId, string userName)
        {
            this.badgeId = badgeId;
            this.userName = userName;
        }

        [PrimaryKey, NotNull]
        public int id { get; set; }
        [Indexed, NotNull]
        public string userName { get; set; }
        //[NotNull]
        public string data { get; set; }
        [Indexed, NotNull]
        public int badgeId { get; set; }
        [NotNull]
        public double percent { get; set; }
        public string progressColor { get; set; }
        [NotNull]
        public int year { get; set; }
        public string tint { get; set; }
        //[NotNull]
        public bool? seen { get; set; }
        [NotNull]
        public DateTime createdAt { get; set; }
        [NotNull]
        public DateTime updatedAt { get; set; }
    }
}
