using System;
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


        private static IWebSocket socket; //websocket used by service

        public const int WaitDelayInterval = 500; //milliseconds to pause for anything that waits on another thread to provide input
        public const int LongTimeout = 10000; //timeout for calls we expect return values from
        public const int ShortTimeout = 250; //timeout for quick calls or items we don't expect values from
        public const int QuickPause = 50; //timeout to allow calls to settle that don't need waited on.

        private static List<int> SubscriptionIds = new List<int>();  //list of subscription id's managed by Service

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

            if (socket != null)
            {
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
                        await Task.Delay(WaitDelayInterval); //check every 1/2 second
                    }
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

            //logged in steps
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                //subscriptions
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
                if (socket != null)
                {
                    if (socket.IsConnected)
                    {
                        socket.Disconnect();
                    }
                }

            }

            //remove all subscriptions
            SubscriptionIds.Clear();

            //terminate the websocket, if needed
            await DisconnectWebsocket();

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

        public static async Task<DabServiceWaitResponse> RegisterUser(string FirstName, string LastName, string EmailAddress, string Password)
        {
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send register mutation
            string command = $"mutation {{registerUser(email: \"{EmailAddress}\", firstName: \"{FirstName}\", lastName: \"{LastName}\", password: \"{Password}\"){{ token }}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.RegisterUser);

            //return the response
            return response;

        }

        public static async Task<DabServiceWaitResponse> UpdateToken()
        {
            //update a users's token

            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //send update token mutation
            var command = "mutation { updateToken(version: 1) { token } }";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.UpdateToken);

            //return the response
            return response;

        }

        public static async Task<DabServiceWaitResponse> ResetPassword(string Email)
        {
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //send update token mutation
            var command = $"mutation {{ resetPassword(email: \"{Email}\" )}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.ResetPassword);

            //return the response
            return response;
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

        public static async Task<DabServiceWaitResponse> SaveUserProfile(string FirstName, string LastName, string EmailAddress)
        {
            //this routine updates a user's profile

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);


            var command = $"mutation {{ updateUserFields(firstName: \"{FirstName}\", lastName: \"{LastName}\", email: \"{EmailAddress}\") {{ id wpId firstName lastName nickname email language channel channels userRegistered token }}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload));
            socket.Send(JsonIn);

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.SaveUserProfile);

            return response;
        }

        public static async Task<DabServiceWaitResponse> ChangePassword(string OldPassword, string NewPassword)
        {

            //this routine updates a user's password

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);


            var command = $"mutation {{ updatePassword( currentPassword: \"{OldPassword}\" newPassword: \"{NewPassword}\")}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            var JsonIn = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload));
            socket.Send(JsonIn);

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.ChangePassword);

            return response;

        }

        //CHANNELS AND EPISODES

        public static async Task<DabServiceWaitResponse> GetEpisodes(DateTime StartDate, int ChannelId)
        {
            //TODO: get episodes since the last updated date and wait for response.
            //TODO: this will need to handle the loops put in place with cursors and may require more arguments
            throw new NotImplementedException();
        }


        //ACTIONS

        public enum ServiceActionsEnum
        {
            Favorite,
            Listened,
            Journaled,
            PositionChanged
        }

        public static async Task<DabServiceWaitResponse> LogAction(int EpisodeId, ServiceActionsEnum Action, double Position = 0)
        {
            //TODO: log an action to Service and wait for confirmation it was processed.
            throw new NotImplementedException();
        }

        public static async Task<DabServiceWaitResponseList> GetActions(DateTime StartDateGmt)
        {
            /*
             * this routine gets actions since a given date (GMT)
             * note that this routine is different as it returns a LIST of responses as it builds all actions together
             */

            //check for a connecting before proceeding
            if (!IsConnected)
            {
                return new DabServiceWaitResponseList()
                {
                    Success = false,
                    ErrorMessage = "Not Connected"
                };
            }

            //prep for handling a loop of actions
            List<DabGraphQlRootObject> result = new List<DabGraphQlRootObject>();
            bool getMoreActions = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMoreActions == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = $"{{lastActions(date: \"{StartDateGmt.ToString("o")}Z\") {{ edges {{ id episodeId userId favorite listen position entryDate updatedAt createdAt }} pageInfo {{ hasNextPage endCursor }} }} }} ";

                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = $"{{lastActions(date: \"{StartDateGmt.ToString("o")}Z\", cursor: \"{cursor }\") {{ edges {{ id episodeId userId favorite listen position entryDate updatedAt createdAt }} pageInfo {{ hasNextPage endCursor }} }} }}";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetActions);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.lastActions;

                    //add what we receied to the list
                    result.Add(response.Data);

                    //determine if we have more data to process or not
                    if (data.pageInfo.hasNextPage == true)
                    {
                        cursor = data.pageInfo.endCursor;
                    }
                    else
                    {
                        //nomore data - break the loop
                        getMoreActions = false;
                    }
                }
                else
                {
                    //something went wrong - return an error message (still in a list)
                    return new DabServiceWaitResponseList()
                    {
                        Success = false,
                        ErrorMessage = response.ErrorMessage
                    };

                }

            }

            return new DabServiceWaitResponseList()
            {
                Success = true,
                Data = result
            };

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

            //TODO: Consider replacing this with a wait for response, but it doesn't appear we get responses when establishing subscriptions, so just a slight delay here for now.
            ////Wait for appropriate response
            //var service = new DabServiceWaitService();
            //var response = await service.WaitForServiceResponse(DabServiceWaitTypes.StartSubscription, ShortTimeout);
            await Task.Delay(QuickPause);
            DabServiceWaitResponse response = new DabServiceWaitResponse(new DabGraphQlRootObject() { type = "complete" }); //imitation ql response

            //return the response
            return response;
        }

        private static void Socket_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            //Handle incoming subscription notifications we care about

            DabGraphQlRootObject ql = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);
            DabGraphQlData data = ql?.payload?.data;

            //capture service-wide issues
            if (ql.type == "connection_error")
            {
                if (ql.payload?.message == "Your token is not valid.")
                {
                    HandleInvalidToken(ql);
                }
            }

            if (ql.type=="ka")
            {
                //keepalive message
                HandleKeepAlive();
                //don't do anything else
                return;
            }


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

        private static async void HandleInvalidToken(DabGraphQlRootObject ql)
        {
            /*
             * Handle an invalid token message, meaning we probably need to log the user out
             */

            Debug.WriteLine($"INVALIDTOKEN");
            throw new NotImplementedException();
        }

        private static async void HandleKeepAlive()
        {
            /* 
             * Handle a basic keep-alive message
             */

            await DabServiceRoutines.NotifyOnConnectionKeepAlive();
        }

    }
}
