using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.LastEpisodeDateQueryHelper
{
    public class Edge
    {
        public string id { get; set; }
        public int episodeId { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string notes { get; set; }
        public string author { get; set; }
        public DateTime date { get; set; }
        public string audioURL { get; set; }
        public int audioSize { get; set; }
        public int audioDuration { get; set; }
        public string audioType { get; set; }
        public string readURL { get; set; }
        public string readTranslationShort { get; set; }
        public string readTranslation { get; set; }
        public int channelId { get; set; }
        public int? unitId { get; set; }
        public int year { get; set; }
        public object shareURL { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}
