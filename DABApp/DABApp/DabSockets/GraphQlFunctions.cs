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

            GlobalResources.WaitStart("Connecting to DAB Servers...");

            //TODO: Check for a QraphQL Connection

            //Send the Login mutation

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
            var response = await service.WaitForGraphQlObject("connection_init", 10000);

            //Return to the calling app
            GlobalResources.WaitStop();
            return response;

        }

        public static async Task<GraphQlWaitResponse> GetUserData (string token)
        {
            //This routine takes a token and gets the user profile information from it.

            GlobalResources.WaitStart("Getting your user profile");

            //TODO: Check for a QraphQL Connection

            //Send the Login mutation
            string command = $"query {{user{{wpId,firstName,lastName,email}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("user", 10000);

            //Return to the calling app
            GlobalResources.WaitStop();
            return response;
        }

        public static async Task<GraphQlWaitResponse> LoginUser(string email, string password)
        {
            //This routine takes a specified username and password and attempts to log the user in via graphql.

            GlobalResources.WaitStart("Checking your credentials...");

            //TODO: Check for a QraphQL Connection

            //Send the Login mutation
            string command = $"mutation {{loginUser(email: \"{email}\", password: \"{password}\", version: 1) {{token}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("loginUser", 10000);

            //Return to the calling app
            GlobalResources.WaitStop();
            return response;

        }
    }
}
