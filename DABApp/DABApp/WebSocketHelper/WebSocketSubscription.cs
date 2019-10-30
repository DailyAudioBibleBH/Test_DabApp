using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.WebSocketHelper
{
    class WebSocketSubscription
    {
        public string type { get; set; }
        public Payload payload { get; set; }
        public string id { get; set; }

        public WebSocketSubscription(string type, Payload payload)
        {
            this.type = type;
            this.payload = payload;
        }
    }
}
