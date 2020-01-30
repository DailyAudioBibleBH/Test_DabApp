using System;
namespace DABApp.DabSockets
{
    public class DabGraphQlMessageEventHandler
    {
        public string message;

        public DabGraphQlMessageEventHandler()
        { }

        public DabGraphQlMessageEventHandler(string message)
        {
            //Init with values
            this.message = message;
        }
    }
}
