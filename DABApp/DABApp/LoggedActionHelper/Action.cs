using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.LoggedActionHelper
{
    public class Action
    {
        public int userId { get; set; }
        public int episodeId { get; set; }
        public bool? listen { get; set; }
        public int? position { get; set; }
        public bool? favorite { get; set; }
        public object entryDate { get; set; }
    }
}
