using System;
namespace DABApp.DabSockets
{
    public class GraphQlWaitResponse
    {

        public bool Success = false;
        public string ErrorMessage = "";
        public DabGraphQlRootObject data = null;
        public GraphQlWaitResponse()
        {
        }
    }
}
