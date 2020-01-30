using System;
namespace DABApp.DabSockets
{
    public class DabGraphQlMessageEventHandler
    {
        public string Message;

        public DabGraphQlMessageEventHandler()
        { }

        public DabGraphQlMessageEventHandler(string message)
        {
            //Init with values
            this.Message = message;
        }
    }
}
