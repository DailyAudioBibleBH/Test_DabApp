using System;
namespace DABApp.DabSockets
{
    public class GraphQlWaitResponse
    {

        public bool Result = false;
        public string ErrorMessage = "";
        public DabGraphQlRootObject data = null;

        public GraphQlWaitResponse()
        {
        }
    }
}
