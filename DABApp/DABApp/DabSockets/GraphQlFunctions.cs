using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace DABApp.DabSockets
{
    public static class GraphQlFunctions
    {
        public static async Task<GraphQlWaitResponse> InitializeConnection(string Token)
        {
            //This routine init's a new connection with the token.

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

            Payload token = new Payload(Token, origin);
            var ConnectInit = JsonConvert.SerializeObject(new ConnectionInitSyncSocket("connection_init", token));
            DabSyncService.Instance.Send(ConnectInit);

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("connection_init"); //smaller timeout in case we don't get ack.. move along

            return response;

        }

        public static async Task<GraphQlWaitResponse> AddSubscription(int id, string subscriptionJson)
        {
            //This routine takes a subscription Json string and subscribes to it. It waits for it to finish before returning

            DabGraphQlPayload payload = new DabGraphQlPayload(subscriptionJson, new DabGraphQlVariables());
            var SubscriptionInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", payload, id));
            DabSyncService.Instance.subscriptionIds.Add(id);
            DabSyncService.Instance.Send(SubscriptionInit);


            //Wait for appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("subscription");

            return response;


        }

        public static async Task<GraphQlWaitResponse> GetUserData (string token)
        {
            //This routine takes a token and gets the user profile information from it.

            //TODO: Check for a QraphQL Connection

            //Send the Login mutation
            string command = $"query {{user{{wpId,firstName,lastName,email}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("user");

            return response;
        }

        public static async Task<GraphQlWaitResponse> CheckEmail(string email)
        {
            //this method takes an email and checks to see if it is for a new or existing user

            //send the query
            const string quote = "\"";
            string command = "query { checkEmail(email:" + quote + email + quote + " )}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //wait for appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("checkemail");

            return response;



        }

        public static async Task<GraphQlWaitResponse> LoginUser(string email, string password)
        {
            //This routine takes a specified username and password and attempts to log the user in via graphql.

            //TODO: Check for a QraphQL Connection

            //Send the Login mutation
            string command = $"mutation {{loginUser(email: \"{email}\", password: \"{password}\", version: 1) {{token}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("loginUser");

            return response;

        }
    }
}
