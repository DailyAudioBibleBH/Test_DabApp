using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DABApp.DabSockets
{

    public enum GraphQlWaitTypes
    {
        /* This enum contains the list of various methods that can be waited on.
         * Each of these items will have a switch/case section in the listener on the websocket
         */
        InitConnection,
        CheckEmail,
        LoginUser,
        GetUserProfile,
        StartSubscription
    }

    public class GraphQlWaitService
    {
        /* This service class connects itself to listen to specific types of messages on the websocket.
         * When a message is received that cooresponds to the type of method being listened for, a response
         * is built, and the "waiting" flag is set to false so that the WHILE loop knows it can stop listening
         * and return the appropriate response. If the timeout expires, the method will also return with a false
         * message along with a timeout error.
         */

        GraphQlWaitTypes _waitType; //type of wait being performed
        string _error = ""; //friendly error message to return
        DabGraphQlRootObject _qlObject = null; //starts null
        bool _waiting = true; //start off true, will set to false once ready

        public GraphQlWaitService()
        {
            //no constructor needed
        }

        public async Task<GraphQlWaitResponse> WaitForGraphQlObject(GraphQlWaitTypes WaitType, int TimeoutMilliseconds = 20000)
        {
            /* this method listens-loops-returns the value to the calling app as a task.
             * it should be awaited in the calling method unless it is an intentional fire-and-forget
             * type of task
             */
            
            _waitType = WaitType;

            //Connect a listener
            DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;

            //wait for the timeout period or until a result is sound (sets waiting to false)
            DateTime start = DateTime.Now;
            DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
            while (_waiting == true && DateTime.Now < timeout)
            {
                TimeSpan remaining = timeout.Subtract(DateTime.Now);
                Debug.WriteLine($"Waiting {remaining.ToString()} for {WaitType} - Wait: {_waiting} - Result: {JsonConvert.SerializeObject(_qlObject)}");
                await Task.Delay(500); //check every 1/2 second
            }

            //Disconnect the listener
            DabSyncService.Instance.DabGraphQlMessage -= Instance_DabGraphQlMessage;

            //Return the appropriate response
            GraphQlWaitResponse result; //result to be returned

            if (_qlObject != null) //result found
            {
                result = new GraphQlWaitResponse(_qlObject);
            }

            else if (_qlObject == null && _error == "") //timeout expired
            {
                result = new GraphQlWaitResponse(GraphQlErrorResponses.TimeoutOccured);
            }

            else if (_error != "") //error received
            {
                result = new GraphQlWaitResponse(GraphQlErrorResponses.CustomError, _error);
            }

            else //other unexpected result
            {
                result = new GraphQlWaitResponse(GraphQlErrorResponses.UnknownErrorOccurred);
            }

            return result;


        }

        private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            /* this is the handler of incoming socket messages that will compare the wait type to the type of 
             * message being processed and handle appropriate messages as needed
             */
            {
                try
                {
                    //deserialize the message into an object
                    DabGraphQlRootObject response = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                    switch (_waitType) //one of the enum values
                    {
                        //Login Processor Messages
                        case GraphQlWaitTypes.LoginUser:
                            //successful login
                            if (response?.payload?.data?.loginUser != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }

                            //error during login
                            if (response?.payload?.errors != null)
                            {
                                //Find the relevant error
                                var loginError = response.payload.errors.Where(x => x.path.Contains("loginUser")).FirstOrDefault();
                                if (loginError != null)
                                {
                                    _error = loginError.message;
                                    _waiting = false;
                                }
                            }
                            break;

                        case GraphQlWaitTypes.GetUserProfile:
                            //successful user profile reception
                            if (response?.payload?.data?.user != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case GraphQlWaitTypes.InitConnection:
                            //successful connection
                            if (response?.type == "connection_ack")
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        default:
                            //ignore this response, it's not relevant.
                            break;

                        case GraphQlWaitTypes.StartSubscription:
                            //successful subscription
                            if (response.type=="complete")
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case GraphQlWaitTypes.CheckEmail:
                            //check email query finished
                            if (response?.payload?.data?.checkEmail != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //ignore the message and keep waiting...
                    Debug.WriteLine($"Exception processing message: {ex.Message}");
                }

            };
        }
    }
}
