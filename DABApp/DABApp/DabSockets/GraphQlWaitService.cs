using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DABApp.DabSockets
{

    public enum GraphQlWaitTypes
    {
        InitConnection,
        CheckEmail,
        LoginUser,
        GetUserProfile,
        StartSubscription
    }

    public class GraphQlWaitService
    {
        public GraphQlWaitService()
        {
        }

        GraphQlWaitTypes _waitType;
        string _error = ""; //friendly error message to return
        DabGraphQlRootObject _qlObject = null; //starts null
        bool _waiting = true; //start off true, will set to false once ready


        public async Task<GraphQlWaitResponse> WaitForGraphQlObject(GraphQlWaitTypes WaitType, int TimeoutMilliseconds = 2000)
        {
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

            GraphQlWaitResponse result; //result to be returned

            //Return the appropriate response
            if (_qlObject != null) //result found
            {
                result = new GraphQlWaitResponse(_qlObject);
            }

            else if (_qlObject == null && _error == "") //timeout expired
            {
                result = new GraphQlWaitResponse(GraphQlErrorResponses.TimeoutOccured);
            }

            else if (_error != null) //error received
            {
                result = new GraphQlWaitResponse(GraphQlErrorResponses.CustomError, _error);
            }

            else //other unexpected result
            {
                result = new GraphQlWaitResponse(GraphQlErrorResponses.UnknownErrorOccurred);
            }

            Debug.WriteLine($"Returning QL Result: {JsonConvert.SerializeObject(result)}");
            return result;


        }

        private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            {
                try
                {
                    DabGraphQlRootObject response = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                    switch (_waitType) //will be upper case
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
                            //TOOD: GraphQlConnected needs replaced?
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
                    Debug.WriteLine($"Exception processing message: {ex.Message}");
                }

            };
        }
    }
}
