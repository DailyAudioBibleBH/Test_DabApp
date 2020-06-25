using System;
using System.Collections.Generic;
using DABApp.DabSockets;

namespace DABApp.Service
{
    public enum DabServiceErrorResponses
    {
        TimeoutOccured,
        UnknownErrorOccurred,
        Disconnected,
        CustomError,
        None
    }

    public class DabServiceWaitResponseList
    {
        /* This result type is similar to a normal WaitResponse except
         * it contains a list of GraphQL data instead of only 1
         */

        public bool Success = false;
        public string ErrorMessage = "";
        public List<DabGraphQlRootObject> Data = new List<DabGraphQlRootObject>();


        public DabServiceWaitResponseList()
        {
            //generic constructur
        }



    }

    public class DabServiceWaitResponse
    {

        public bool Success = false; //true = successful response - false = something didn't go as expected
        public string ErrorMessage = ""; //custom error emssage if success is false
        public DabServiceErrorResponses ErrorType = DabServiceErrorResponses.None; //type of error, if there is one
        public DabGraphQlRootObject Data = null; //the graphql object that is to be used on success


        public DabServiceWaitResponse()
        {
            /* 
             * generic constructor
             */
        }

        public DabServiceWaitResponse(DabGraphQlRootObject GraphQlData)
        {
            /* 
             * constructor with a success result (includes GraphQL data)
             */
            Data = GraphQlData;
            Success = true;
            ErrorMessage = "";
            ErrorType = DabServiceErrorResponses.None;
        }

        public DabServiceWaitResponse(DabServiceErrorResponses ErrorType, string CustomErrorMessage = "")
        {
            /*
             * constructor with error messages built in
             */

            this.ErrorType = ErrorType;

            switch (ErrorType)
            {
                case DabServiceErrorResponses.Disconnected:
                    //graphql is not connected
                    Data = null;
                    ErrorMessage = "The Daily Audio Bible service is currently unavailable.";
                    Success = false;
                    break;

                case DabServiceErrorResponses.TimeoutOccured:
                    //timeout expired
                    Data = null;
                    ErrorMessage = "Timeout occured while waiting for a response.";
                    Success = false;
                    break;

                case DabServiceErrorResponses.UnknownErrorOccurred:
                    //unknown error occured
                    Success = false;
                    Data = null;
                    ErrorMessage = "Unknown error occured.";
                    break;

                case DabServiceErrorResponses.CustomError: //handled by default error handler also.
                default:
                    //custom / generic error message
                    Success = false;
                    Data = null;
                    ErrorMessage = (CustomErrorMessage != "") ? CustomErrorMessage : "An error occured while communicating with the Daily Audio Bible servers.";
                    break;
            }

        }

    }
}
