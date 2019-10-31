using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.WebSocketHelper
{
    public class FirstEpisodeCompare
    {
        public string listen { get; set; }
        public int position { get; set; }
        public bool favorite { get; set; }

        public FirstEpisodeCompare(string listen, int position, bool favorite)
        {
            this.listen = listen;
            this.position = position;
            this.favorite = favorite;
        }
    }
}
