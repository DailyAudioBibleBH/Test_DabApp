using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.DabSockets
{
    public class Payload
    {
        //public string __invalid_name__x-token { get; set; }
        public string x_token {get; set;}

        public Payload(string x_token)
        {
            this.x_token = x_token;
        }
    }
}
