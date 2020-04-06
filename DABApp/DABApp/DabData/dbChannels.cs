using System;
using SQLite;

namespace DABApp
{
    [Table("Channel")]
    public class dbChannels
    {
        [PrimaryKey, Indexed]
        public string id { get; set; }
        [Indexed]
        public int channelId { get; set; }
        public string key { get; set; }
        [Indexed]
        public string title { get; set; }
        public string imageURL { get; set; }
        public int rolloverMonth { get; set; }
        public int rolloverDay { get; set; }
        public int bufferPeriod { get; set; }
        public int bufferLength { get; set; }
        public bool @public { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}
