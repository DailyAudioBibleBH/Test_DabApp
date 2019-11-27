using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabSockets
{
    public class Payload
    {
        //public string __invalid_name__x-token { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "x-token")]
        public string xtoken {get; set;}

        public Payload(string xtoken)
        {
            this.xtoken = xtoken;
        }
    }
}
