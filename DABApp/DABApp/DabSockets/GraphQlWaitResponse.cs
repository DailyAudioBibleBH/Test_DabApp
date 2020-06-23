using System;
namespace DABApp.DabSockets
{
    public enum GraphQlErrorResponses
    {
        TimeoutOccured,
        UnknownErrorOccurred,
        Disconnected,
        CustomError
    }

    public class GraphQlWaitResponse
    {

        public bool Success = false;
        public string ErrorMessage = "";
        public DabGraphQlRootObject Data = null;


        public GraphQlWaitResponse()
        {
            /* 
             * generic constructor
             */
        }

        public GraphQlWaitResponse(DabGraphQlRootObject GraphQlData)
        {
            /* 
             * constructor with a success result (includes GraphQL data)
             */
            Data = GraphQlData;
            Success = true;
            ErrorMessage = "";
        }

        public GraphQlWaitResponse(GraphQlErrorResponses ErrorType, string ErrorMessage = "")
        {
            /*
             * constructor with error messages built in
             */

            switch (ErrorType)
            {
                case GraphQlErrorResponses.Disconnected:
                    //graphql is not connected
                    Data = null;
                    ErrorMessage = "The Daily Audio Bible service is currently unavailable.";
                    Success = false;
                    break;

                case GraphQlErrorResponses.TimeoutOccured:
                    //timeout expired
                    Data = null;
                    ErrorMessage = "Timeout occured while waiting for a response.";
                    Success = false;
                    break;

                case GraphQlErrorResponses.UnknownErrorOccurred:
                    //unknown error occured
                    Success = false;
                    Data = null;
                    ErrorMessage = "Unknown error occured.";
                    break;

                case GraphQlErrorResponses.CustomError: //handled by default error handler also.
                default:
                    //custom / generic error message
                    Success = false;
                    Data = null;
                    ErrorMessage = (ErrorMessage != "") ? ErrorMessage : "An error occured while communicating with the Daily Audio Bible servers.";
                    break;
            }

        }

    }
}
