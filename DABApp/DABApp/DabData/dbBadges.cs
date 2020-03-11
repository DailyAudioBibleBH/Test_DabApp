using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp
{
    [Table("Badge")]
    public class dbBadges
    {
        [PrimaryKey, NotNull]
        public int badgeId { get; set; }
        [NotNull]
        public string name { get; set; }
        [NotNull]
        public string description { get; set; }
        [NotNull]
        public string imageURL { get; set; }
        public string type { get; set; }
        public string method { get; set; }
        public string data { get; set; }
        [NotNull]
        public bool visible { get; set; }
        [NotNull]
        public DateTime createdAt { get; set; }
        [NotNull]
        public DateTime updatedAt { get; set; }
    }
}
