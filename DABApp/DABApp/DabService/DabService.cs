﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DABApp.DabSockets;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace DABApp.Service
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
        public const int LongTimeout = 5000;
        public const int ShortTimeout = 250;
        private static List<int> SubscriptionIds = new List<int>();

        //WEBSOCKET CONNECTION

        public static IWebSocket Socket
        {
            //public reference to the socket for connecting events and such
            get
            {
                return socket;
            }
        }

        private static async Task<bool> ConnectWebsocket(int TimeoutMilliseconds = LongTimeout)
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

        private static async Task<bool> DisconnectWebsocket(int TimeoutMilliseconds = LongTimeout)
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

        //GRAPHQL CONNECTION

        public static bool IsConnected
        {
            get
            {
                if (socket == null)
                {
                    return false;
                }
                return (socket.IsConnected); //as long as the socket is connected, we should be good to go.
            }
        }

        public static async Task<DabServiceWaitResponse> InitializeConnection()
        {
            /* this routine inits a new connection without a token and determines which one to use based on login state
             */
            string token = dbSettings.GetSetting("Token", "");
            if (token == "")
            {
                //use the api token
                return await InitializeConnection(GlobalResources.APIKey);
            }
            else
            {
                return await InitializeConnection(token);
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

            //set up appropriate subscriptions

            //Generic subscriptions
            var ql = await Service.DabService.AddSubscription(1, "subscription { episodePublished { episode { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } } }");
            ql = await Service.DabService.AddSubscription(2, "subscription { badgeUpdated { badge { badgeId name description imageURL type method data visible createdAt updatedAt } } }");

            //logged in subscriptions
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                ql = await Service.DabService.AddSubscription(3, "subscription { actionLogged { action { id userId episodeId listen position favorite entryDate updatedAt createdAt } } }");
                ql = await Service.DabService.AddSubscription(4, "subscription { tokenRemoved { token } }");
                ql = await Service.DabService.AddSubscription(5, "subscription { progressUpdated { progress { id badgeId percent year seen createdAt updatedAt } } }");
                ql = await Service.DabService.AddSubscription(6, "subscription { updateUser { user { id wpId firstName lastName email language } } } ");
            }

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
                foreach (int id in SubscriptionIds)
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

        // AUTHENTICATION

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

        public static async Task<DabServiceWaitResponse> RegisterUser(string FirstName, string LastName, string EmailAddress)
        {
            //TODO: Handle this messaging and wait for the correct response, and then update user properties
            throw new NotImplementedException();
        }

        public static async Task<DabServiceWaitResponse> UpdateToken()
        {
            //TODO: Update a user 's token
            throw new NotImplementedException();
        }

        public static async Task<DabServiceWaitResponse> ResetPassword()
        {
            //TODO: Send command to reset user's password
            throw new NotImplementedException();
        }


        //USER PROFILE

        public static async Task<DabServiceWaitResponse> GetUserData()
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

        public static async Task<DabServiceWaitResponse> SaveUserData(string FirstName, string LastName, string EmailAddress)
        {
            //TODO: send command and wait for response.
            throw new NotImplementedException();
        }

        public static async Task<DabServiceWaitResponse> ChangePassword (string OldPassword, string NewPassword)
        {
            //TODO: change the users's password and wait for the response.
            throw new NotImplementedException();
        }

        //CHANNELS AND EPISODES

        public static async Task<DabServiceWaitResponse> GetEpisodes(DateTime StartDate, int ChannelId)
        {
            //TODO: get episodes since the last updated date and wait for response.
            //TODO: this will need to handle the loops put in place with cursors and may require more arguments
            throw  new NotImplementedException();
        }


        //ACTIONS

        public enum ServiceActionsEnum
        {
            Favorite,
            Listened,
            Journaled,
            PositionChanged
        }

        public static async Task<DabServiceWaitResponse> LogAction(int EpisodeId, ServiceActionsEnum Action, double Position = 0 )
        {
            //TODO: log an action to Service and wait for confirmation it was processed.
            throw new NotImplementedException();
        }

        public static async Task<DabServiceWaitResponse> GetActions(DateTime StartDate)
        {
            //TODO: get last actions since a date.
            //TODO: this procedure needs to handle the looping necessary and may need more arguments
            throw new NotImplementedException();
        }

        //BADGES AND PROGRESS

        public static async Task<DabServiceWaitResponse> SeeProgress(int ProgressId)
        {
            //TODO: send progress seen and wait for confiramation
            throw new NotImplementedException();
        }

        //SUBSCRIPTIONS

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
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.StartSubscription, ShortTimeout);

            //return the response
            return response;
        }

        private static void Socket_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            //Handle incoming subscription notifications we care about

            DabGraphQlRootObject ql = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);
            DabGraphQlData data = ql?.payload?.data;

            //exit the method if nothing to process
            if (data == null)
            {
                return;
            }

            //Identify subscriptions we need to deal with

            if (data.actionLogged != null)
            {
                //new action logged!
                HandleActionLogged(data.actionLogged);

            }
            else if (data.episodePublished != null)
            {
                //new episode published
                HandleEpisodePublished(data.episodePublished);

            }
            else if (data.progressUpdated != null)
            {
                //progress updated
                HandleProgressUpdated(data.progressUpdated);

            }
            else if (data.tokenRemoved != null)
            {
                //token removed

            }
            else if (data.updateUser != null)
            {
                //user profile updated
                Debug.WriteLine($"USER: {e.Message}");

            }
            else
            {
                //nothing to see here... all other incoming messages should be handled by the appropriate wait service
            }

        }

        private static async void HandleActionLogged(DabGraphQlActionLogged data)
        {
            /* 
             * Handle an incoming action log
             */

            Debug.WriteLine($"ACTIONLOGGED: {JsonConvert.SerializeObject(data)}");

            //TODO: Handle this by storing the action in the database and sending messaging out so any UI will know to update

        }

        private static async void HandleEpisodePublished(DabGraphQlEpisodePublished data)
        {
            /* 
             * Handle an incoming episode notification
             */

            Debug.WriteLine($"EPISODEPUBLISHED: {JsonConvert.SerializeObject(data)}");

            //TODO: Handle this by adding episode to the database and sending messaging out to notify UI to update

        }

        private static async void HandleProgressUpdated(DabGraphQlProgressUpdated data)
        {
            /*
             * Handle an incoming pogress update notification
             */

            Debug.WriteLine($"PROGRESSUPDATED: {JsonConvert.SerializeObject(data)}");

            //TODO: Handle this by updating database (don't think we have any UI notifications here

        }

        private static async void HandleTokenRemoved(TokenRemoved data)
        {
            /*
             * Handle an incoming token removed update notification
             */

            Debug.WriteLine($"TOKENREMOVED: {JsonConvert.SerializeObject(data)}");

            //TODO: Handle this by logging the user out. Within that method, we should terminate the connection and reset it to the generic API token

        }

        private static async void HandleUpdateUser(DabGraphQlUpdateUser data)
        {
            /* 
             * Handle an incoming update user notification by updating user profile data and making any UI notifications
             */

            Debug.WriteLine($"UPDATEUSER: {JsonConvert.SerializeObject(data)}");

            //TODO: Handle this

        }

    }
}
