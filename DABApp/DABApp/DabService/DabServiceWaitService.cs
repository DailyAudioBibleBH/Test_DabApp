using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DABApp.DabSockets;
using Newtonsoft.Json;

namespace DABApp.Service
{

    public enum DabServiceWaitTypes
    {
        /* This enum contains the list of various methods that can be waited on.
         * Each of these items will have a switch/case section in the listener on the websocket
         */
        InitConnection,
        CheckEmail,
        LoginUser,
        GetUserProfile,
        StartSubscription,
        RegisterUser,
        UpdateToken,
        ResetPassword,
        ChangePassword,
        SaveUserProfile,
        GetActions,
        GetEpisodes,
        GetChannels,
        GetBadges,
        LogAction
    }

    public class DabServiceWaitService
    {
        /* This service class connects itself to listen to specific types of messages on the websocket.
         * When a message is received that cooresponds to the type of method being listened for, a response
         * is built, and the "waiting" flag is set to false so that the WHILE loop knows it can stop listening
         * and return the appropriate response. If the timeout expires, the method will also return with a false
         * message along with a timeout error.
         */

        DabServiceWaitTypes _waitType; //type of wait being performed
        string _error = ""; //friendly error message to return
        DabGraphQlRootObject _qlObject = null; //starts null
        bool _waiting = true; //start off true, will set to false once ready

        public DabServiceWaitService()
        {
            //no constructor needed
        }

        public async Task<DabServiceWaitResponse> WaitForServiceResponse(DabServiceWaitTypes WaitType, int TimeoutMilliseconds = DabService.LongTimeout)
        {
            /* this method listens-loops-returns the value to the calling app as a task.
             * it should be awaited in the calling method unless it is an intentional fire-and-forget
             * type of task
             */
            
            _waitType = WaitType;

            //Connect a listener
            DabService.Socket.DabGraphQlMessage += Service_GraphQlMessage;

            //wait for the timeout period or until a result is sound (sets waiting to false)
            DateTime start = DateTime.Now;
            DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
            while (_waiting == true && DateTime.Now < timeout)
            {
                TimeSpan remaining = timeout.Subtract(DateTime.Now);
                Debug.WriteLine($"Waiting {remaining.ToString()} for {WaitType} - Wait: {_waiting} - Result: {JsonConvert.SerializeObject(_qlObject)}");
                await Task.Delay(DabService.WaitDelayInterval); //wait interval
            }

            //Disconnect the listener
            DabService.Socket.DabGraphQlMessage -= Service_GraphQlMessage;

            //Return the appropriate response
            DabServiceWaitResponse result; //result to be returned

            if (_qlObject != null) //result found
            {
                result = new DabServiceWaitResponse(_qlObject);
            }

            else if (_qlObject == null && _error == "") //timeout expired
            {
                result = new DabServiceWaitResponse(DabServiceErrorResponses.TimeoutOccured);
            }

            else if (_error != "") //error received
            {
                result = new DabServiceWaitResponse(DabServiceErrorResponses.CustomError, _error);
            }

            else //other unexpected result
            {
                result = new DabServiceWaitResponse(DabServiceErrorResponses.UnknownErrorOccurred);
            }

            return result;


        }

        private void Service_GraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            /* this is the handler of incoming socket messages that will compare the wait type to the type of 
             * message being processed and handle appropriate messages as needed
             */
            {
                try
                {
                    //deserialize the message into an object
                    DabGraphQlRootObject response = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                    if (response.type=="ka")
                    {
                        //nothing to do...
                        return;
                    }

                    switch (_waitType) //one of the enum values
                    {
                        //Login Processor Messages
                        case DabServiceWaitTypes.LoginUser:
                            //successful login
                            if (response?.payload?.data?.loginUser != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                                break;
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
                                    break;
                                }
                            }
                            break;

                        case DabServiceWaitTypes.GetUserProfile:
                            //successful user profile reception
                            if (response?.payload?.data?.user != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.InitConnection:
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

                        case DabServiceWaitTypes.StartSubscription:
                            //successful subscription
                            if (response.type=="complete")
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.CheckEmail:
                            //check email query finished
                            if (response?.payload?.data?.checkEmail != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                                break;
                            }

                            //error during check email
                            if (response?.payload?.errors != null)
                            {
                                //Find the relevant error
                                var error = response.payload.errors.FirstOrDefault();
                                if (error != null)
                                {
                                    _error = error.message;
                                    _waiting = false;
                                    break;
                                }

                            }
                            break;

                        case DabServiceWaitTypes.RegisterUser:
                            //registration finished
                            if (response?.payload?.data?.registerUser != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.UpdateToken:
                            //update token finished
                            if (response?.payload?.data?.updateToken != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.ResetPassword:
                            //reset password finished
                            if (response?.payload?.data?.resetPassword != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.ChangePassword:
                            //change password finished
                            if (response?.payload?.data?.updatePassword != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                                break;
                            }

                            //error during check email
                            if (response?.payload?.errors != null)
                            {
                                //Find the relevant error
                                var error = response.payload.errors.FirstOrDefault();
                                if (error != null)
                                {
                                    _error = error.message;
                                    _waiting = false;
                                    break;
                                }

                            }
                            break;

                        case DabServiceWaitTypes.SaveUserProfile:
                            //user profile saved
                            if (response?.payload?.data?.updateUserFields != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.GetActions:
                            //actions received
                            if (response?.payload?.data?.lastActions != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.GetEpisodes:
                            //episodes received
                            if (response?.payload?.data?.episodes != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.GetChannels:
                            //channels received
                            if (response?.payload?.data?.channels != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.GetBadges:
                            //badges received
                            if (response?.payload?.data?.updatedBadges != null)
                            {
                                _qlObject = response;
                                _waiting = false;
                            }
                            break;

                        case DabServiceWaitTypes.LogAction:
                            //action logged
                            if (response?.payload?.data?.logAction != null)
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
