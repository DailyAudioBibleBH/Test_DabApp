using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp
{
    public class dbBadgeProgress
    {
        [PrimaryKey, NotNull]
        public int id { get; set; }
        [NotNull]
        public string data { get; set; }
        [Indexed, NotNull]
        public int badgeId { get; set; }
        [NotNull]
        public double percent { get; set; }
        [NotNull]
        public int year { get; set; }
        [NotNull]
        public bool seen { get; set; }
        [NotNull]
        public DateTime createdAt { get; set; }
        [NotNull]
        public DateTime updatedAt { get; set; }
    }
}
