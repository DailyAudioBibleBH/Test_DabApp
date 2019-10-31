using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DABApp.LoggedActionHelper
{
    public class ActionLoggedRootObject
    {
        public string type { get; set; }
        public object id { get; set; }
        public Payload payload { get; set; }
    }
}
