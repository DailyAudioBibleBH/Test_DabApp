using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using DABApp.DabUI;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Services;
using SQLite;
using Xamarin.Forms;
using static DABApp.ContentConfig;

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

        public const int ExtraLongTimeout = 20000; //super long timeout for things that take a long time
        public const int LongTimeout = 10000; //timeout for calls we expect return values from
        public const int SocketTerminationWaitTime = 1000; //time to wait for socket terminate time to close the socket
        public const int ShortTimeout = 250; //timeout for quick calls or items we don't expect values from
        public const int QuickPause = 50; //timeout to allow calls to settle that don't need waited on.
        private static List<int> SubscriptionIds = new List<int>();  //list of subscription id's managed by Service
        public static string userName;
        public static object cursur { get; set; } = null;


        //DATABASE CONNECTION
        static SQLiteAsyncConnection adb = DabData.AsyncDatabase;//Async database to prevent SQLite constraint errors


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
            try
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
                    Debug.WriteLine($"Connecting websocket to {uri}...");
                    socket.Connect();

                    //Wait for the socket to connect
                    DateTime start = DateTime.Now;
                    DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
                    while (socket.IsConnected == false && DateTime.Now < timeout)
                    {
                        TimeSpan remaining = timeout.Subtract(DateTime.Now);
                        Debug.WriteLine($"Waiting {remaining.ToString()} for socket connection...");
                        await Task.Delay(WaitDelayInterval); //check every 1/2 second
                    }

                }

                //return final state of the socket
                return socket.IsConnected;
            }
            catch (Exception)
            {
                //something went wrong while trying to connect, return not connected.
                return false;
            }
            
        }

        internal static async Task<DabServiceWaitResponseList> GetCampaigns(DateTime LastDate)
        {
            /*
            * this routine gets all the campaigns since a given date
            * this routine uses pagination
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
            bool getMore = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { updatedCampaigns(date: \"" + LastDate.ToString("o") + "Z\") { edges { id wpId title description status suggestedSingleDonation suggestedRecurringDonation pricingPlans default } pageInfo { hasNextPage endCursor } } }";
                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { updatedCampaigns(date: \"" + LastDate.ToString("o") + "Z\", cursor: \"" + cursor + "\"){ edges { id wpId title description status suggestedSingleDonation suggestedRecurringDonation pricingPlans default } pageInfo { hasNextPage endCursor } } }";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetUpdatedCampaigns);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.updatedCampaigns;

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
                        getMore = false;
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

        public static async Task<DabServiceWaitResponseList> GetUserDonationHistoryUpdate(DateTime LastDate)
        {
            /*
            * this routine checks for user specific donation updates
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
            bool getMore = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { updatedDonationHistory(date: \"" + LastDate.ToString("o") + "Z\") { edges { id wpId platform paymentType chargeId date donationType currency grossDonation fee netDonation campaignWpId userWpId } pageInfo { hasNextPage endCursor } } }";

                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { updatedDonationStatus(date: \"" + LastDate.ToString("o") + "Z\", cursor: \"" + cursor + "\") { edges { id wpId platform paymentType chargeId date donationType currency grossDonation fee netDonation campaignWpId userWpId } pageInfo { hasNextPage endCursor } } }";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetDonationHistory);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.updatedDonationHistory;

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
                        getMore = false;
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

        public static async Task<DabServiceWaitResponseList> GetUserDonationStatusUpdate(DateTime LastDate)
        {
            /*
            * this routine checks for user specific donation updates
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
            bool getMore = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { updatedDonationStatus(date: \"" + LastDate.ToString("o") + "Z\") { edges { id wpId source amount recurringInterval campaignWpId userWpId status } pageInfo { hasNextPage endCursor } } }";

                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { updatedDonationStatus(date: \"" + LastDate.ToString("o") + "Z\", cursor: \"" + cursor + "\") { edges { id wpId source amount recurringInterval campaignWpId userWpId status } pageInfo { hasNextPage endCursor } } }";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetDonationStatuses);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.updatedDonationStatus;

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
                        getMore = false;
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

        public static async Task<Forum> GetForum(bool fromPullToRefresh = false)
        {
            try
            {
                var forum = new Forum();
                DabGraphQlUpdatedForum activeForum = GlobalResources.ActiveForum;
                forum.id = activeForum.wpId;
                forum.title = activeForum.title;
                forum.topicCount = activeForum.topicCount;
                DabServiceWaitResponseList result;
                if (fromPullToRefresh)
                    result = await DabService.GetUpdatedTopics(GlobalResources.ActiveForumId, 100, null);
                else
                    result = await DabService.GetUpdatedTopics(GlobalResources.ActiveForumId, 100, cursur);

                List<DabGraphQlTopic> topics = new List<DabGraphQlTopic>();
                if (result.Success)
                {
                    foreach (var item in result.Data)
                    {
                        topics = item.payload.data.updatedTopics.edges.Where(x => x.status == "publish").OrderByDescending(x => x.lastActive).ToList();
                    }
                }
                ObservableCollection<DabGraphQlTopic> topicCollection = new ObservableCollection<DabGraphQlTopic>(topics);
                forum.topics = topicCollection;

                return forum;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<DabServiceWaitResponse> PostTopic(ContentConfig.PostTopic topic)
        {
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the update donation mutation
            string command = $"mutation {{ createTopic( forumWpId: {topic.forumId} title: \"{topic.title}\" content: \"{topic.content}\" ) {{ wpId userWpId forumWpId title content voiceCount replyCount type status userNickname }} }}";

            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.CreateTopic);

            //return the response
            return response;
        }

        public static async Task<DabServiceWaitResponse> PostReply(PostReply rep)
        {
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the update donation mutation
            string command = $"mutation {{ createReply(topicWpId: {rep.topicId} content: \"{rep.content}\") {{ wpId userWpId topicWpId content status userNickname }} }}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.CreateReply);

            //return the response
            return response;
        }

        public static async Task<DabServiceWaitResponseList> GetUpdatedReplies(DateTime LastDate, int wpId, int limit)
        {
            /*
            * this routine checks for replies related to a topic
            */

            DependencyService.Get<IAnalyticsService>().LogEvent("prayerwall_post_read");

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
            bool getMore = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { updatedReplies(date: \"" + LastDate.ToString("o") + "Z\", topicWpId: " + wpId + ", limit: " + limit + ") { edges { wpId userWpId topicWpId content status userNickname userReplies userTopics createdAt updatedAt } pageInfo { hasNextPage endCursor } } } ";

                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { updatedReplies(date: \"" + LastDate.ToString("o") + "Z\", cursor: \"" + cursor + "\", topicWpId: " + wpId + ", limit: " + limit + ") { edges { wpId userWpId topicWpId content status userNickname createdAt updatedAt } pageInfo { hasNextPage endCursor } } } ";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetReplies);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.updatedReplies;

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
                        getMore = false;
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

        public static async Task<DabServiceWaitResponseList> GetUpdatedTopics(int forumWpId, int forumLimit, object cursor = null)
        {
            /*
            * this routine checks for updated topics for forum
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
            DateTime LastDate = DateTime.Now;
            LastDate = LastDate.AddYears(-1);
            //Send the command
            string command;
            if (cursor == null)
            {
                //First run
                command = "query { updatedTopics(date: \"" + LastDate.ToString("o") + "\", forumWpId: " + forumWpId +", limit: " + forumLimit +") { edges { wpId userWpId forumWpId title content voiceCount replyCount type status updatedAt createdAt lastActive userNickname userReplies userTopics } pageInfo { hasNextPage endCursor } } }";

            }
            else
            {
                //Subsequent runs, use the cursor
                command = "query { updatedTopics(date: \"" + LastDate.ToString("o") + "\", cursor: \"" + cursor + "\", forumWpId: " + forumWpId + ", limit: " + forumLimit + ") { edges { wpId userWpId forumWpId title content voiceCount replyCount type status updatedAt createdAt lastActive userNickname userReplies userTopics } pageInfo { hasNextPage endCursor } } }";
            }
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetTopics);

            //Process the actions
            if (response.Success == true)
            {
                var data = response.Data.payload.data.updatedTopics;

                //add what we receied to the list
                result.Add(response.Data);

                //determine if we have more data to process or not
                if (data.pageInfo.hasNextPage == true)
                {
                    DabService.cursur = data.pageInfo.endCursor;
                }
                else
                {
                    //nomore data - break the loop
                    DabService.cursur = null;
                }
            }
            else
            {
                //something went wrong - return an error message (still in a list)
                return new DabServiceWaitResponseList()
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                };

            }

            return new DabServiceWaitResponseList()
            {
                Success = true,
                Data = result,
                //Cursor = newCursor
            };

        }

        public static async Task<DabServiceWaitResponseList> GetUpdatedForums(DateTime LastDate)
        {
            /*
            * this routine gets all the users updated credit card information
            */
            LastDate = DateTime.MinValue;


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

            //start a loop to get all actions
            //Send the command
            string command;

            //First run
            command = "query { updatedForums(date: \"" + LastDate.ToString("o") + "Z\") { wpId title content excerpt topicCount replyCount type status visibility } } ";


            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetForums);

            //Process the actions
            if (response.Success == true)
            {
                //add what we receied to the list
                result.Add(response.Data);
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

            return new DabServiceWaitResponseList()
            {
                Success = true,
                Data = result
            };
        }

        public static async Task<DabServiceWaitResponseList> GetUsersUpdatedCreditCards(DateTime LastDate)
        {
            /*
            * this routine gets all the users updated credit card information
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

            //start a loop to get all actions
            //Send the command
            string command;

            //First run
            command = "query { updatedCards(date: \"" + LastDate.ToString("o") + "Z\") { wpId userId lastFour expMonth expYear type status } }";
                
          
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetCreditCardProgresses);

            //Process the actions
            if (response.Success == true)
            {
                //add what we receied to the list
                result.Add(response.Data);
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

            return new DabServiceWaitResponseList()
            {
                Success = true,
                Data = result
            };
        }

        public static async Task<DabServiceWaitResponseList> GetUserProgress(DateTime LastDate)
        {
            /*
            * this routine gets all the users progress towards badges for the app
            * this routine uses pagination
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
            bool getMore = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { updatedProgress(date: \"" + LastDate.ToString("o") + "Z\") { edges { id badgeId percent seen year createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { updatedProgress(date: \"" + LastDate.ToString("o") + "Z\", cursor: \"" + cursor + "\"){ edges { id badgeId percent seen year createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetBadgeProgresses);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.updatedProgress;

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
                        getMore = false;
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

        private static async Task<bool> DisconnectWebsocket(int TimeoutMilliseconds = LongTimeout)
        {
            //disconnects and resets the websocket

            if (socket != null)
            {
                //disconnect the event listeners
                socket.DabSocketEvent -= Socket_DabSocketEvent;
                socket.DabGraphQlMessage -= Socket_DabGraphQlMessage;

                if (socket.IsConnected == true)
                {

                    //disconnect the socket
                    socket.Disconnect();

                    //wait for the socket to become disconnected
                    DateTime start = DateTime.Now;
                    DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
                    while (socket.IsConnected == false && DateTime.Now < timeout)
                    {
                        TimeSpan remaining = timeout.Subtract(DateTime.Now);
                        Debug.WriteLine($"Waiting {remaining.ToString()} for socket connection to close...");
                        await Task.Delay(WaitDelayInterval); //check every 1/2 second
                    }

                    if (socket.IsConnected)
                    {
                        Debug.WriteLine("Socket error - the socket was unable to disconnect in the timeout provided."); //TODO: Breakpoint here ,this should not happen.
                    }
                }
            }

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
            string token = GlobalResources.Instance.LoggedInUser.Token;
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
            ql = await Service.DabService.AddSubscription(3, "subscription { campaignUpdated { campaign { id wpId title description status suggestedSingleDonation suggestedRecurringDonation pricingPlans default}}}");

            //logged in steps
            if (GuestStatus.Current.IsGuestLogin == false)
            {
                //subscriptions
                ql = await Service.DabService.AddSubscription(4, "subscription { actionLogged { action { id userId episodeId listen position favorite entryDate updatedAt createdAt } } }");
                ql = await Service.DabService.AddSubscription(5, "subscription { tokenRemoved { token } }");
                ql = await Service.DabService.AddSubscription(6, "subscription { progressUpdated { progress { id badgeId percent year seen createdAt updatedAt } } }");
                ql = await Service.DabService.AddSubscription(7, "subscription { updateUser { user { id wpId firstName lastName email language } } } ");
                ql = await Service.DabService.AddSubscription(8, "subscription { updatedCard { card { wpId userId lastFour expMonth expYear type status } } }");
                ql = await Service.DabService.AddSubscription(9, "subscription { donationStatusUpdated { donationStatus { id wpId source amount recurringInterval campaignWpId userWpId status }}}");
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
                await Task.Delay(SocketTerminationWaitTime);

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

        public static async Task<DabServiceWaitResponse> GetAddresses ()
        {
            /*
             * This routine requests a list of addresses assigned to the user, billing and shipping.
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the Login mutation
            string command = $"query {{ addresses {{ wpId type firstName lastName company addressOne addressTwo city state postcode country phone email}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetAddresses);

            //return the response
            return response;
        }

        public static async void TestUpdateCampaign(int ProgressId)
        {
            //var camp = adb.Table<dbCampaigns>().ToListAsync().Result;
            //DabGraphQlVariables variables = new DabGraphQlVariables();
            ////string command = "mutation { createCampaign(wpId: 124, title: \"TESTING\", description: \"You’re invited!\", suggestedSingleDonation: 100.00, suggestedRecurringDonation: 25.00, status: \"publish\", pricingPlans: null ) { id wpId title description status suggestedSingleDonation suggestedRecurringDonation pricingPlans default}}";

            ////string command = "mutation { deleteCampaign(wpId: 460159) { id wpId title description status suggestedSingleDonation suggestedRecurringDonation pricingPlans default } }";

            ////“[{\"type\":\"Weekly\",\"amount\":1,\"id\":\"price_1HIdmgDIA4tgn0DSqmq1bfZh\",\"recurring\":true},{\"type\":\"Single\",\"amount\":100,\"id\":\"price_1HHZp4DIA4tgn0DSbdwWTMkf\",\"recurring\":false},{\"type\":\"Monthly\",\"amount\":100,\"id\":\"Campaign_One\",\"recurring\":true}]”
            ////, description: “You’re invited!”, suggestedSingleDonation: 100.00, suggestedRecurringDonation: 25.00, status: “publish”, pricingPlans: null
            //string command = "mutation { updateCampaign(wpId: 102899, title: \"Daily Audio Bible\", description: \" \", suggestedSingleDonation: 100.00, suggestedRecurringDonation: 25.00, status: \"publish\", pricingPlans: [{type:\"Weekly\", amount:1, id:\"price_1HIdmgDIA4tgn0DSqmq1bfZh\", recurring :true},{type:\"Single\", amount :100, id :\"price_1HHZp4DIA4tgn0DSbdwWTMkf\", recurring :false},{type:\"Monthly\", amount :100, id :\"Campaign_One\", recurring :true}]) { id wpId title description status suggestedSingleDonation suggestedRecurringDonation pricingPlans default }}";
            //var payload = new DabGraphQlPayload(command, variables);
            //socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));
        }

        public static bool UpdateDonation(string quantity, string type, int cardId, int campaignWpId, string next)
        {
            /*
             * This routine takes a specified wpId and attempts to update a donation via graphql
             */

            //check for a connecting before proceeding
            if (!IsConnected) return false;
            //Send the update donation mutation
            string command = $"mutation {{updateDonation(quantity: {quantity}, donationType: \"{type}\", cardId: {cardId}, campaignWpId: {campaignWpId}, nextPaymentDate: \"{next}\") }}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));
            return true;
        }

        public static async Task<DabServiceWaitResponse> CreateDonation(string quantity, string type, int cardId, int campaignWpId, string next)
        {
            /*
             * This routine takes a specified wpId and attempts to create a donation via graphql
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the update donation mutation
            string command = $"mutation {{createDonation(quantity: {quantity}, donationType: {type}, cardId: {cardId}, campaignWpId: {campaignWpId}, nextPaymentDate: \"{next}\") {{token}}}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.CreateDonation);

            //return the response
            return response;
        }

        public static bool DeleteDonation(int id)
        {
            /*
             * This routine takes a specified wpId and attempts to delete a donation via graphql
             */

            //check for a connecting before proceeding
            if (!IsConnected) return false;

            //Send the update donation mutation
            string command = $"mutation {{deleteDonation(campaignWpId: {id}) }}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));
            return true;
        }

        public static async Task<DabServiceWaitResponse> DeleteCard(int wpId)
        {
            /*
             * This routine takes a specified wpId and attempts to delete a card via graphql
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the delete card mutation
            string command = $"mutation {{deleteCard(wpId: {wpId}) }}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.DeleteCard);

            //return the response
            return response;
        }

        public static async Task<DabServiceWaitResponse> AddCard(StripeContainer result)
        {
            const string quote = "\"";

            /*
             * This routine takes a specified wpId and attempts to delte a card via graphql
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send the Login mutation
            string command = $"mutation {{addCard(processor: " + quote + "stripe" + quote+ ", processorData: " + quote + $"{result.card_token}" + quote + ") {wpId userId lastFour expMonth expYear type status }}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.UpdatedCard);

            //return the response
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

        public static async Task<DabServiceWaitResponse> UpdateUserAddress(Address address)
        {
            /*
             * This routine takes a specified username and password and attempts to log the user in via graphql.
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            if (address.type == "billing")
            {
                //Send the Billing mutation
                var command = $"mutation {{ updateUserAddress(type: \"{address.type}\" firstName: \"{address.first_name}\", lastName: \"{address.last_name}\", company: \"{address.company}\", addressOne: \"{address.address_1}\", addressTwo: \"{address.address_2}\", city: \"{address.city}\", state: \"{address.state}\", postcode: \"{address.postcode}\", country: \"{address.country}\", phone: \"{address.phone}\", email: \"{address.email}\") {{ wpId type firstName lastName company addressOne addressTwo city state postcode country phone email }}}}";
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));
            }
            else
            {
                //Send the Shipping mutation
                var command = $"mutation {{ updateUserAddress(type: \"{address.type}\" firstName: \"{address.first_name}\", lastName: \"{address.last_name}\", company: \"{address.company}\", addressOne: \"{address.address_1}\", addressTwo: \"{address.address_2}\", city: \"{address.city}\", state: \"{address.state}\", postcode: \"{address.postcode}\", country: \"{address.country}\") {{ wpId type firstName lastName company addressOne addressTwo city state postcode country }}}}";
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));
            }
            

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.UpdateUserAddress);

            //return the response
            return response;
        }

        public static async Task<DabServiceWaitResponse> RegisterUser(string FirstName, string LastName, string EmailAddress, string Password)
        {
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //Send register mutation

            string command = $"mutation {{registerUser(email: \"{EmailAddress}\", firstName: \"{FirstName}\", lastName: \"{LastName}\", password: \"{Password}\"){{ id wpId firstName lastName nickname email language channel channels userRegistered token }}}}";
            var mRegister = new DabGraphQlPayload(command, new DabGraphQlVariables());
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", mRegister)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.RegisterUser,ExtraLongTimeout); //Added longer wait time to register user since it was not recieving a response fast enough

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
            string command = $"query {{user{{id wpId firstName lastName nickname email language channel channels userRegistered token}}}}";
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

        public static async Task<DabServiceWaitResponse> GetChannels()
        {
            /*
             * this routine gets all the channels for the app
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);


            var command  = "query { channels { id channelId key title imageURL rolloverMonth rolloverDay bufferPeriod bufferLength public createdAt updatedAt}}";
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            var json = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload));
            socket.Send(json);

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetChannels);

            return response;


        }

        public static async Task<DabServiceWaitResponseList> GetEpisodes(DateTime StartDateUtc, int ChannelId)
        {
            //Figuring if this is first run of GetEpisodes or not
            string queryName;
            bool isFirstTime;
            DabGraphQlEpisodes data;

            if (StartDateUtc <= GlobalResources.DabMinDate.ToUniversalTime())
            {
                queryName = "episodes"; //first time through, use episodes
                isFirstTime = true;
            }
            else
            {
                queryName = "updatedEpisodes"; //not first time through, use updatedepisodes
                isFirstTime = false;
            }
                

            //this routine gets episodes for a channel
            //it returns a list of ql objects

            //check for a connecting before proceeding
            if (!IsConnected)
            {
                return new DabServiceWaitResponseList()
                {
                    Success = false,
                    ErrorMessage = "Not Connected"
                };
            }

            //prep for handling a loop of episodes
            List<DabGraphQlRootObject> result = new List<DabGraphQlRootObject>();
            bool getMore = true;
            object cursor = null;

            //start a loop to get all episodes
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { " + queryName + "(date: \"" + StartDateUtc.ToString("o") + "Z\", channelId: " + ChannelId + ") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";

                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { " + queryName + "(date: \"" + StartDateUtc.ToString("o") + "Z\", channelId: " + ChannelId + ", cursor: \"" + cursor + "\") { edges { id episodeId type title description notes author date audioURL audioSize audioDuration audioType readURL readTranslationShort readTranslation channelId unitId year shareURL createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetEpisodes, LongTimeout);

                //Process the episodes
                if (response.Success == true)
                {
                    if (isFirstTime)
                        data = response.Data.payload.data.episodes; //first time through, use episodes
                    else
                        data = response.Data.payload.data.updatedEpisodes; //later times through, use updatedepisdoes


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
                        getMore = false;
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



        //ACTIONS

        public enum ServiceActionsEnum
        {
            Favorite,
            Listened,
            Journaled,
            PositionChanged
        }

        public static async Task<DabServiceWaitResponse> LogAction(int EpisodeId, ServiceActionsEnum Action, DateTime ActionDate, bool? BoolValue, int? IntValue)
        {
            /*
             * this routine logs an action to the service
             */

            //check for a connecting before proceeding
            if (!IsConnected) return new DabServiceWaitResponse(DabServiceErrorResponses.Disconnected);

            //build the command
            string command;
            var updatedAt = ActionDate.ToString("o") + "Z";
            switch (Action)
            {
                case ServiceActionsEnum.Favorite:
                    if (! BoolValue.HasValue) throw new NotSupportedException("No favorite value provided.");
                    command = $"mutation {{logAction(episodeId: {EpisodeId}, favorite: {BoolValue.Value.ToString().ToLower()}, updatedAt: \"{updatedAt}\") {{episodeId userId favorite updatedAt favoriteUpdatedAt}}}}";
                    break;
                case ServiceActionsEnum.Listened:
                    if (!BoolValue.HasValue) throw new NotSupportedException("No listened value provided.");
                    command = $"mutation {{logAction(episodeId: {EpisodeId}, listen: {BoolValue.Value.ToString().ToLower()}, updatedAt: \"{updatedAt}\") {{episodeId userId listen updatedAt listenUpdatedAt}}}}";
                    break;
                case ServiceActionsEnum.PositionChanged:
                    if (!IntValue.HasValue) throw new NotSupportedException("No position value provided.");
                    command = $"mutation {{logAction(episodeId: {EpisodeId}, position: {IntValue.Value}, updatedAt: \"{updatedAt}\") {{episodeId userId position updatedAt positionUpdatedAt}}}}";
                    break;
                case ServiceActionsEnum.Journaled:
                    //TODO: Implement this
                    string entryDate = DateTime.Now.ToString("yyyy-MM-dd");
                    if (!BoolValue.HasValue) throw new NotSupportedException("No journal value provided.");
                    command = "mutation {logAction(episodeId: " + EpisodeId + ", entryDate: \"" + entryDate + "\", updatedAt: \"" + updatedAt + "\") {episodeId userId entryDate updatedAt entryDateUpdatedAt}}";
                    break;
                default:
                    //Nothing to do here, unuspported action style
                    throw new NotImplementedException();
            }

            //Send the command
            var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
            var json = JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload));
            socket.Send(json);

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.LogAction);

            //return the response
            return response;

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

        public static async Task<DabServiceWaitResponseList> GetUpdatedBadges(DateTime LastDate)
        {
            /*
            * this routine gets all the badges for the app
            * this routine uses pagination
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
            bool getMore = true;
            object cursor = null;

            //start a loop to get all actions
            while (getMore == true)
            {
                //Send the command
                string command;
                if (cursor == null)
                {
                    //First run
                    command = "query { updatedBadges(date: \"" + LastDate.ToString("o") + "Z\") { edges { badgeId id name description imageURL type method visible createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";

                }
                else
                {
                    //Subsequent runs, use the cursor
                    command = "query { updatedBadges(date: \"" + LastDate.ToString("o") + "Z\", cursor: \"" + cursor + "\") { edges { badgeId id name description imageURL type method visible createdAt updatedAt } pageInfo { hasNextPage endCursor } } }";
                }
                var payload = new DabGraphQlPayload(command, new DabGraphQlVariables());
                socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

                //Wait for the appropriate response
                var service = new DabServiceWaitService();
                var response = await service.WaitForServiceResponse(DabServiceWaitTypes.GetBadges);

                //Process the actions
                if (response.Success == true)
                {
                    var data = response.Data.payload.data.updatedBadges;

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
                        getMore = false;
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

        public static async Task<DabServiceWaitResponse> SeeProgress(int ProgressId)
        {
            DabGraphQlVariables variables = new DabGraphQlVariables();
            string command = "mutation { seeProgress(id:" + ProgressId + ") { id badgeId percent year seen } }";
            var payload = new DabGraphQlPayload(command, variables);
            socket.Send(JsonConvert.SerializeObject(new DabGraphQlCommunication("start", payload)));

            //Wait for the appropriate response
            var service = new DabServiceWaitService();
            var response = await service.WaitForServiceResponse(DabServiceWaitTypes.SeeProgress);

            //return the response
            return response;
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
            //var response = await service.WaitForServiceResponse(DabServiceWaitTypes.StartSubscription);
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
                HandleTokenRemoved(data.tokenRemoved);

            }
            else if (data.updateUser != null)
            {
                //user profile updated
                HandleUpdateUser(data.updateUser.user);

            }
            else if (data.updatedCard != null)
            {
                //credit card updated
                HandleUpdateCreditCard(data.updatedCard.card);
            }
            else if (data.donationStatusUpdated != null)
            {
                //user donation updated
                HandleUpdateDonation(data.donationStatusUpdated.donationStatus);
            }
            //else if (data.updateDonation != null)
            //{
            //    HandleDonationSuccessMessage(data.updateDonation);
            //}
            //else if (data.deleteDonation != null)
            //{
            //    HandleDeleteDonationSuccessMessage(data.deleteDonation);
            //}
            else if (data.updateCampaign != null)
            {
                //campaign updated
                HandleUpdatedCampaign(data.updateCampaign);
            }
            else if (data.deleteCampaign != null)
            {
                //campaign deleted
                HandleUpdatedCampaign(data.deleteCampaign);
            }
            else if (data.campaignUpdated != null)
            {
                //campaign updated
                DabGraphQlUpdateCampaign camp = new DabGraphQlUpdateCampaign(data.campaignUpdated);
                HandleUpdatedCampaign(camp);
            }
            else if (data.createCampaign != null)
            {
                //new campaign created
                HandleUpdatedCampaign(data.createCampaign);
            }
            else
            {
                //nothing to see here... all other incoming messages should be handled by the appropriate wait service
            }

        }

        private static async void HandleUpdateDonation(DabGraphQlDonation data)
        {
            /* 
             * Handle an incoming donation update
             */

            await DabServiceRoutines.ReceiveDonationUpdate(data);
        }

        private static async void HandleActionLogged(DabGraphQlActionLogged data)
        {
            /* 
             * Handle an incoming action log
             */

            await DabServiceRoutines.ReceiveActionLog(data.action);
        }

        private static async void HandleUpdatedCampaign(DabGraphQlUpdateCampaign data)
        {
            /* 
             * Handle an incoming campaign update
             */

            await DabServiceRoutines.RecieveCampaignUpdate(data);
        }

        private static async void HandleEpisodePublished(DabGraphQlEpisodePublished data)
        {
            /* 
             * Handle an incoming episode notification
             */

            await DabServiceRoutines.EpisodePublished(data.episode);
        }

        //Progress was made
        private static async void HandleProgressUpdated(DabGraphQlProgressUpdated data)
        {
            /*
             * Handle an incoming pogress update notification
             */

            await DabServiceRoutines.UpdateProgress(data);

            
        }

        private static async void HandleTokenRemoved(TokenRemoved data)
        {
            /*
             * Handle an incoming token removed update notification
             */

            Debug.WriteLine($"TOKENREMOVED: {JsonConvert.SerializeObject(data)}");

            await DabServiceRoutines.RemoveToken();
        }

        private static async void HandleUpdateUser(GraphQlUser data)
        {
            /* 
             * Handle an incoming update user notification by updating user profile data and making any UI notifications
             */

            await DabServiceRoutines.UpdateUserProfile(data);

        }

        private static async void HandleUpdateCreditCard(DabGraphQlCreditCard data)
        {
            /* 
             * Handle an incoming update credit card notification by updating user credit card data and making any UI notifications
             */

            await DabServiceRoutines.UpdateCreditCard(data);
        }

        private static async void HandleInvalidToken(DabGraphQlRootObject ql)
        {
            /*
             * Handle an invalid token message, meaning we probably need to log the user out
             */

            await GlobalResources.LogoffAndResetApp();
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
