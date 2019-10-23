using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabSockets
{
    public class ConnectionInitSyncSocket
    {
        public string type { get; set; }
        public Payload payload { get; set; }

        public ConnectionInitSyncSocket(string type, Payload payload)
        {
            this.type = type;
            this.payload = payload;
        }
    }
}
