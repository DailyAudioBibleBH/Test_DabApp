using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.WebSocketHelper
{
    public class Payload
    {
        public string query { get; set; }
        public Variables variables { get; set; }

        public Payload(string query, Variables variables)
        {
            this.query = query;
            this.variables = variables;
        }
    }
}
