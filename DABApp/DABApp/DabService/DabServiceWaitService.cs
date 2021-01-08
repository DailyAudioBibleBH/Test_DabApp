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
        LogAction,
        GetAddresses,
        UpdateUserAddress,
        SeeProgress,
        GetBadgeProgresses,
        GetCreditCardProgresses,
        UpdatedCard
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
        bool _firstWait = true; //indicator first time through the loop to show detail...
        bool _multipleWaits = false; // indicator of multiple waits for the debugger

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
                if (_firstWait == true)
                {
                    Debug.WriteLine($"Waiting {remaining.ToString()} for {WaitType}:");
                }
                else
                {
                    Debug.Write($"{remaining.TotalSeconds:0.0}|"); //write to same line to reduce debugger clutter
                    _multipleWaits = true;
                }
                await Task.Delay(DabService.WaitDelayInterval); //wait interval
                _firstWait = false;
            }

            if (_multipleWaits) Debug.WriteLine("");//end of line if we wrote to the same line earlier

            //Disconnect the listener
            DabService.Socket.DabGraphQlMessage -= Service_GraphQlMessage;

            //Return the appropriate response
            DabServiceWaitResponse result; //result to be returned

            if (_error != "") //error received
            {
                result = new DabServiceWaitResponse(DabServiceErrorResponses.CustomError, _error);
            }

            else if (_qlObject != null) //result found
            {
                result = new DabServiceWaitResponse(_qlObject);
            }

            else if (_qlObject == null && _error == "") //timeout expired (no specific error)
            {
                result = new DabServiceWaitResponse(DabServiceErrorResponses.TimeoutOccured);
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
                //deserialize the message into an object
                DabGraphQlRootObject response = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                if (response.type == "ka")
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

                    case DabServiceWaitTypes.GetBadgeProgresses:
                        if (response?.payload?.data?.updatedProgress != null)
                        {
                            _qlObject = response;
                            _waiting = false;
                        }
                        break;

                    case DabServiceWaitTypes.GetCreditCardProgresses:
                        if (response?.payload?.data?.updatedCards != null)
                        {
                            _qlObject = response;
                            _waiting = false;
                        }
                        break;

                    case DabServiceWaitTypes.SeeProgress:
                        if (response?.payload?.data?.seeProgress != null)
                        {
                            _qlObject = response;
                            _waiting = false;
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
                            break;
                        }
                        if (response?.type == "connection_error")
                        {
                            _waiting = false;
                            _error = response.payload.message;
                            break;
                        }
                        break;

                    case DabServiceWaitTypes.StartSubscription:
                        //successful subscription
                        if (response.type == "complete")
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
                        if (response?.payload?.data?.updatedEpisodes != null || response?.payload?.data?.episodes != null)
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
                            break;
                        }
                        if (response?.payload?.errors != null)
                        {
                            if (response?.payload?.errors.Count() > 0)
                            {
                                //check for logaction errors we should deal with
                                foreach (var error in response.payload.errors)
                                {
                                    if (error.path.Contains("logAction"))
                                    {
                                        _error = error.message;
                                        _waiting = false;
                                        break;
                                    }
                                }
                            }
                            break;
                        }

                        break;
                    case DabServiceWaitTypes.GetAddresses:
                        //addresses recieved
                        if (response?.payload?.data?.addresses != null)
                        {
                            _qlObject = response;
                            _waiting = false;
                        }
                        break;
                    case DabServiceWaitTypes.UpdateUserAddress:
                        //update address response recieved
                        if (response?.payload?.data?.updateUserAddress != null)
                        {
                            _qlObject = response;
                            _waiting = false;
                        }
                        break;
                    case DabServiceWaitTypes.UpdatedCard:
                        //delete card response received
                        if (response?.payload?.data?.updatedCard != null)
                        {
                            _qlObject = response;
                            _waiting = false;
                        }
                        break;
                }

            };
        }
    }
}
