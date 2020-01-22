using System;
namespace DABApp
{
    public class dbChannels
    {
        public dbChannels()
        {
        }
        public string id { get; set; }
        public int channelId { get; set; }
        public string key { get; set; }
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
