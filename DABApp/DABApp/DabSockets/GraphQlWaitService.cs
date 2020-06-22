using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DABApp.DabSockets
{
    public class GraphQlWaitService
    {
        public GraphQlWaitService()
        {
        }

        string messageCode = "";
        string friendlyError = ""; //friendly error message to return
        DabGraphQlRootObject result = null; //starts null
        bool waiting = true; //start off true, will set to false once ready


        public async Task<GraphQlWaitResponse> WaitForGraphQlObject(string GraphQlMessageCode, int TimeoutMilliseconds = 1000)
        {

            messageCode = GraphQlMessageCode.ToUpper();
            
            //Connect a listener
            DabSyncService.Instance.DabGraphQlMessage += Instance_DabGraphQlMessage;

            //wait for the timeout period or until a result is sound (sets waiting to false)
            DateTime start = DateTime.Now;
            DateTime timeout = DateTime.Now.AddMilliseconds(TimeoutMilliseconds);
            while (waiting == true && DateTime.Now < timeout)
            {
                TimeSpan remaining = timeout.Subtract(DateTime.Now);
                Debug.WriteLine($"Waiting {remaining.ToString()} for {GraphQlMessageCode} - Wait: {waiting} - Result: {JsonConvert.SerializeObject(result)}");
                await Task.Delay(1000);
            }

            //Disconnect the listener
            DabSyncService.Instance.DabGraphQlMessage -= Instance_DabGraphQlMessage;

            //Return the appropriate response
            if (result != null) //result found
            {
                return new GraphQlWaitResponse()
                {
                    Result = true,
                    ErrorMessage = "",
                    data = result
                };
            }

            else if (result == null && friendlyError == "") //timeout expired
            {
                return new GraphQlWaitResponse()
                {
                    Result = false,
                    ErrorMessage = $"Timeout expired after {new TimeSpan(0, 0, 0, 0, TimeoutMilliseconds).TotalSeconds} seconds.",
                    data = null
                };
            }

            else if (friendlyError != null) //error received
            {
                return new GraphQlWaitResponse()
                {
                    Result = false,
                    ErrorMessage = friendlyError,
                    data = null
                };
            }
            else //other unexpected result
            {

                return new GraphQlWaitResponse()
                {
                    Result = false,
                    ErrorMessage = "Unknown Error",
                    data = null
                };
            }


        }

        private void Instance_DabGraphQlMessage(object sender, DabGraphQlMessageEventHandler e)
        {
            {
                try
                {
                    DabGraphQlRootObject response = JsonConvert.DeserializeObject<DabGraphQlRootObject>(e.Message);

                    switch (messageCode) //will be upper case
                    {
                        //Login Processor Messages
                        case "LOGINUSER":
                            //successful login
                            if (response?.payload?.data?.loginUser != null)
                            {
                                result = response;
                                waiting = false;
                            }

                            //error during login
                            if (response?.payload?.errors != null)
                            {
                                //Find the relevant error
                                var loginError = response.payload.errors.Where(x => x.path.Contains("loginUser")).FirstOrDefault();
                                if (loginError != null)
                                {
                                    friendlyError = loginError.message;
                                    waiting = false;
                                }
                            }
                            break;

                        case "USER":
                            //successful user profile reception
                            if (response?.payload?.data?.user != null)
                            {
                                result = response;
                                waiting = false;
                            }
                            break;

                        case "CONNECTION_INIT":
                            //successful connection
                            if (response?.type == "connection_ack")
                            {
                                result = response;
                                waiting = false;
                            }
                            //TOOD: GraphQlConnected needs replaced?
                            break;

                        default:
                            //ignore this response, it's not relevant.
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
