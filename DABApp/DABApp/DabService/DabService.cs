using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.DabSockets;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace DABApp.DabService
{

    public static class DabService
    {
        /* 
         * This static class contains asyncronous methods for various QraphQL
         * service interactions. Most methods will:
         * 1. Check for a connection to the GraphQL service / websocket
         * 2. Send a string of text to the websocket
         * 3. Initiate a "GraphQLWaitService" to wait for the appropriate response, error, or timeout
         * 4. Return the value to the calling method
         */

        
        private static IWebSocket socket;
        private const int DefaultTimeout = 5000;
        private static List<int> SubscriptionIds = new List<int>();

        public static IWebSocket Socket
        {
            //public reference to the socket for connecting events and such
            get
            {
                return socket;
            }
        }

        public static bool IsConnected
        {
            get
            {
                return (socket.IsConnected); //as long as the socket is connected, we should be good to go.
            }
        }

        private static async Task<bool> ConnectWebsocket(int TimeoutMilliseconds = DefaultTimeout)
        {
            //This routine will establish a connection to the websocket if not already established
            if (socket == null)
            {
                //create the socket
                socket = DependencyService.Get<IWebSocket>(DependencyFetchTarget.NewInstance);

            }

            if (socket.IsConnected == false)
            {
                //connect the socket

                //Get the URL to use
                var appSettings = ContentConfig.Instance.app_settings;
                string uri = (GlobalResources.TestMode) ? appSettings.stage_service_link : appSettings.prod_service_link;
                //need to add wss:// since it just gives us the address here
                uri = $"wss://{uri}";

                //Register for socket events
                socket.DabSocketEvent += Socket_DabSocketEvent;
                socket.DabGraphQlMessage += Socket_DabGraphQlMessage;
                //Init the socket
                socket.Init(uri);

                //Connect the socket
                socket.Connect();

                //Wait for the socket to connect
                DateTime start = DateTime.Now;
                DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
                while (socket.IsConnected == false && DateTime.Now < timeout)
                {
                    TimeSpan remaining = timeout.Subtract(DateTime.Now);
                    Debug.WriteLine($"Waiting {remaining.ToString()} for socket connection...");
                    await Task.Delay(500); //check every 1/2 second
                }

            }

            //return final state of the socket
            return socket.IsConnected;
        }

        private static void Socket_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            //TODO: this is where we will handle incoming graphql subscription messages unrelated to functions we await
        }

        private async static void Socket_DabSocketEvent(object sender, DabSocketEventHandler e)
        {
            //An event has been fired by the socket. Respond accordingly

            //Log the event to the debugger
            Debug.WriteLine($"{e.eventName} was fired with {e.data}");

            //Take action on the event
            switch (e.eventName.ToLower())
            {
                case "disconnected": //Socket disconnected
                    await TerminateConnection();
                    break;
                case "connected": //Socket connected
                    //nothing for now
                    break;
                case "reconnecting": //Socket reconnecting
                    //do nothing for now
                    break;
                case "reconnected": //Socket reconnected
                    //re-init the graphql services
                    await TerminateConnection();
                    await InitializeConnection();
                    break;
                case "auth_error": //Error with authentication
                    //nothing for now
                    break;
                default:
                    break;
            }
        }

        private static async Task<bool> DisconnectWebsocket(int TimeoutMilliseconds = DefaultTimeout)
        {
            //disconnects and resets the websocket

            if (socket.IsConnected == true)
            {
                //disconnect the event listeners
                socket.DabSocketEvent -= Socket_DabSocketEvent;
                socket.DabGraphQlMessage -= Socket_DabGraphQlMessage;

                //disconnect the socket
                socket.Disconnect();

                //wait for the socket to become disconnected
                //Wait for the socket to connect
                DateTime start = DateTime.Now;
                DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
                while (socket.IsConnected == false && DateTime.Now < timeout)
                {
                    TimeSpan remaining = timeout.Subtract(DateTime.Now);
                    Debug.WriteLine($"Waiting {remaining.ToString()} for socket connection to close...");
                    await Task.Delay(500); //check every 1/2 second
                }
            }

            //clear the socket reference to reset it completely
            socket = null;

            return true;

        }

        public static async Task<DabServiceWaitResponse> InitializeConnection()
        {
            /* this routine inits a new connection without a token and determines which one to use based on login state
             */
            if (GuestStatus.Current.IsGuestLogin)
            {
                //use the api token
                return await InitializeConnection(GlobalResources.APIKey);
            } else
            {
                return await InitializeConnection(dbSettings.GetSetting("Token", ""));
            }
        }

        public static async Task<DabServiceWaitResponse> InitializeConnection(string Token)
        {
            /*
             * This routine initializes a new connection with the token.
             */

            //establish websocket connection before proceeding
            var connected = await ConnectWebsocket();
            if (connected == false)
            {
                return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected); //websocket disconnected, no way to connect service;
            }
        
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
            socket.Send(ConnectInit);

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.InitConnection); //smaller timeout in case we don't get ack.. move along

            //return the received response
            return response;

        }

        public static async Task<bool> TerminateConnection()
        {
            /*
             * this routine terminates the connection, disconnects the websocket, and clears all subscriptions
             */

            if (IsConnected)
            {

                string command;

                //Unsubscribe from each subscription
                foreach(int id in SubscriptionIds)
                {
                    command = $"{{\"type\":\"stop\",\"id\":\"{id}\",\"payload\":\"null\"}}";
                    socket.Send(command);

                }

                //Terminate the connection
                command = "{\"type\":\"connection_terminate\"}";
                socket.Send(command);

                //wait for the graphql connection to end (which may terminate the socket also)
                await Task.Delay(500);

                //disconnect the socket
                if (socket.IsConnected)
                {
                    socket.Disconnect();
                }

            }

            //remove all subscriptions
            SubscriptionIds.Clear();

            return true;
        }

        public static async Task<DabServiceWaitResponse> AddSubscription(int id, string subscriptionJson)
        {
            /*
             * This routine takes a subscription Json string and subscribes to it. It waits for it to finish before returning
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //prep and send the command
            DabGraphQlPayload payload = new DabGraphQlPayload(subscriptionJson, new DabGraphQlVariables());
            var SubscriptionInit = JsonConvert.SerializeObject(new DabGraphQlSubscription("start", payload, id));
            SubscriptionIds.Add(id);
            socket.Send(SubscriptionInit);

            //Wait for appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.StartSubscription);

            //return the response
            return response;
        }

        public static async Task<DabServiceWaitResponse> GetUserData(string token)
        {
            /*
             * This routine takes a token and gets the user profile information from it.
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the Login mutation
            string command = $"query {{user{{wpId,firstName,lastName,email}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetUserProfile);

            return response;
        }

        public static async Task<DabServiceWaitResponse> CheckEmail(string email)
        {
            /* 
             * this method takes an email and checks to see if it is for a new or existing user
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //send the query
            const string quote = "\"";
            string command = "query { checkEmail(email:" + quote + email + quote + " )}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //wait for appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.CheckEmail);

            //return response
            return response;
        }

        public static async Task<DabServiceWaitResponse> LoginUser(string email, string password)
        {
            /*
             * This routine takes a specified username and password and attempts to log the user in via graphql.
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the Login mutation
            string command = $"mutation {{loginUser(email: \"{email}\", password: \"{password}\", version: 1) {{token}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.LoginUser);

            //return the response
            return response;
        }
    }
}
