using System;
using DABApp.DabSockets;

namespace DABApp.ChannelWebSocketHelper
{
    public class ChannelWebSocketRootObject
    {
        public string type { get; set; }
        public Payload payload { get; set; }
    }
}
