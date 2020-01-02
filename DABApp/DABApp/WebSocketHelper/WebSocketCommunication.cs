using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.WebSocketHelper
{
    public class WebSocketCommunication
    {
        public string type { get; set; }
        public Payload payload { get; set; }

        public WebSocketCommunication(string type, Payload payload)
        {
            this.type = type;
            this.payload = payload;
        }
    }
}
