using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DABApp.DabSockets
{
    public static class GraphQlFunctions
    {
        public static async Task<GraphQlWaitResponse> LoginUser(string email, string password)
        {
            //This routine takes a specified username and password and attempts to log the user in via graphql.

            //Check for a QraphQL Connection

            //Send the Login mutation
            string login = $"mutation {{loginUser(email: \"{email}\", password: \"{password}\", version: 1) {{token}}}}";
            var payload = new DabGraphQlPayload(login, new DabGraphQlVariables());
            DabSyncService.Instance.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new GraphQlWaitService();
            var response = await service.WaitForGraphQlObject("loginUser", 10000);

            //Return to the calling app
            return response;

        }
    }
}
