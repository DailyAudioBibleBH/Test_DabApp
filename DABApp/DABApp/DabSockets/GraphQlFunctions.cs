﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace DABApp.DabSockets
{


    public static class GraphQlFunctions
    {

        public static bool IsGraphQlConnected
        {
            /* 
             * This method indicates whether GraphQl is connected
             */
            get
            {
                //returns true or false if GraphQL is connected
                return DabSyncService.Instance.IsConnected;
            }
        }

        public static async Task<GraphQlWaitResponse> InitializeConnection(string Token)
        {
            /*
             * This routine initializes a new connection with the token.
             */

            //check for a connecting before proceeding
            if (!IsGraphQlConnected) return new GraphQlWaitResponse(GraphQlErrorResponses.Disconnected);

            //set up the origin for the connection
            string origin;
            if (Device.RuntimePlatform == Device.Android)
            {
                origin = "c2it-android";
            }
            else if (Device.RuntimePlatform == Device.iOS)
            {
                origin = "c2it-ios";
            }
            else
            {
                origin = "could not determine runtime platform";
            }

            //json prep
            Payload token = new Payload(Token, origin);
            var ConnectInit = JsonConvert.SerializeObject(new ConnectionInitSyncSocket("connection_init", token));
            DabSyncService.Instance.Send(ConnectInit);

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject(GraphQlWaitTypes.InitConnection); //smaller timeout in case we don't get ack.. move along

            //return the received response
            return response;

        }

        public static async Task<GraphQlWaitResponse> AddSubscription(int id, string subscriptionJson)
        {
            /*
             * This routine takes a subscription Json string and subscribes to it. It waits for it to finish before returning
             */

            //check for a connecting before proceeding
            if (!IsGraphQlConnected) return new GraphQlWaitResponse(GraphQlErrorResponses.Disconnected);

            //prep and send the command
            DabGraphQlPayload payload = new DabGraphQlPayload(subscriptionJson, new DabGraphQlVariables());
            var SubscriptionInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", payload, id));
            DabSyncService.Instance.subscriptionIds.Add(id);
            DabSyncService.Instance.Send(SubscriptionInit);

            //Wait for appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject(GraphQlWaitTypes.StartSubscription);

            //return the response
            return response;
        }

        public static async Task<GraphQlWaitResponse> GetUserData(string token)
        {
            /*
             * This routine takes a token and gets the user profile information from it.
             */

            //check for a connecting before proceeding
            if (!IsGraphQlConnected) return new GraphQlWaitResponse(GraphQlErrorResponses.Disconnected);

            //Send the Login mutation
            string command = $"query {{user{{wpId,firstName,lastName,email}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject(GraphQlWaitTypes.GetUserProfile);

            return response;
        }

        public static async Task<GraphQlWaitResponse> CheckEmail(string email)
        {
            /* 
             * this method takes an email and checks to see if it is for a new or existing user
             */

            //check for a connecting before proceeding
            if (!IsGraphQlConnected) return new GraphQlWaitResponse(GraphQlErrorResponses.Disconnected);

            //send the query
            const string quote = "\"";
            string command = "query { checkEmail(email:" + quote + email + quote + " )}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //wait for appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject(GraphQlWaitTypes.CheckEmail);

            //return response
            return response;
        }

        public static async Task<GraphQlWaitResponse> LoginUser(string email, string password)
        {
            /*
             * This routine takes a specified username and password and attempts to log the user in via graphql.
             */

            //check for a connecting before proceeding
            if (!IsGraphQlConnected) return new GraphQlWaitResponse(GraphQlErrorResponses.Disconnected);

            //Send the Login mutation
            string command = $"mutation {{loginUser(email: \"{email}\", password: \"{password}\", version: 1) {{token}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject(GraphQlWaitTypes.LoginUser);

            //return the response
            return response;
        }
    }
}
